using BookBridge.Data;
using BookBridge.Models.Entities;
using BookBridge.Models.ViewModels;
using BookBridge.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookBridge.Services;

public class BookService : IBookService
{
    private readonly ApplicationDbContext _context;
    private readonly IImageService _imageService;

    public BookService(ApplicationDbContext context, IImageService imageService)
    {
        _context = context;
        _imageService = imageService;
    }

    public async Task<BookListViewModel> GetBooksAsync(BookListViewModel filters)
    {
        var query = _context.Books
            .Include(b => b.Owner)
            .Include(b => b.Category)
            .Include(b => b.Images)
            .Where(b => b.Status != BookStatus.Sold && b.Status != BookStatus.Donated)
            .AsQueryable();

        if (!string.IsNullOrEmpty(filters.SearchQuery))
            query = query.Where(b => b.Title.Contains(filters.SearchQuery) || b.Author.Contains(filters.SearchQuery));

        if (!string.IsNullOrEmpty(filters.Category) && int.TryParse(filters.Category, out int catId))
            query = query.Where(b => b.CategoryId == catId);

        if (!string.IsNullOrEmpty(filters.TransactionType) && Enum.TryParse<TransactionType>(filters.TransactionType, out var tt))
            query = query.Where(b => b.TransactionType == tt);

        if (!string.IsNullOrEmpty(filters.Condition) && Enum.TryParse<BookCondition>(filters.Condition, out var cond))
            query = query.Where(b => b.Condition == cond);

        if (!string.IsNullOrEmpty(filters.City))
            query = query.Where(b => b.City != null && b.City.Contains(filters.City));

        if (filters.MinPrice.HasValue)
            query = query.Where(b => b.Price >= filters.MinPrice.Value);

        if (filters.MaxPrice.HasValue)
            query = query.Where(b => b.Price <= filters.MaxPrice.Value);

        query = filters.SortBy switch
        {
            "price_asc" => query.OrderBy(b => b.Price),
            "price_desc" => query.OrderByDescending(b => b.Price),
            "popular" => query.OrderByDescending(b => b.ViewCount),
            _ => query.OrderByDescending(b => b.CreatedAt)
        };

        int totalCount = await query.CountAsync();
        int pageSize = 12;
        int page = filters.CurrentPage < 1 ? 1 : filters.CurrentPage;

        var books = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new BookListViewModel
        {
            Books = books.Select(MapToCard),
            TotalCount = totalCount,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Categories = await _context.Categories.ToListAsync(),
            SearchQuery = filters.SearchQuery,
            Category = filters.Category,
            TransactionType = filters.TransactionType,
            SortBy = filters.SortBy,
            City = filters.City
        };
    }

    public async Task<Book?> GetBookByIdAsync(int id) =>
        await _context.Books
            .Include(b => b.Owner)
            .Include(b => b.Category)
            .Include(b => b.Images)
            .Include(b => b.Reviews).ThenInclude(r => r.Reviewer)
            .FirstOrDefaultAsync(b => b.Id == id);

    public async Task<BookDetailsViewModel> GetBookDetailsAsync(int id, string? currentUserId)
    {
        var book = await GetBookByIdAsync(id) ?? throw new Exception("Book not found");

        var activeBorrow = await _context.BorrowRequests
            .Where(br => br.BookId == id && br.Status == RequestStatus.Approved)
            .FirstOrDefaultAsync();

        DateTime? availableFrom = null;
        if (activeBorrow != null)
            availableFrom = activeBorrow.DueDate;

        bool hasReviewed = false;
        if (currentUserId != null)
        {
            hasReviewed = await _context.Reviews
                .AnyAsync(r => r.ReviewerId == currentUserId && r.ReviewedUserId == book.OwnerId);
        }

        return new BookDetailsViewModel
        {
            Book = book,
            SimilarBooks = await GetSimilarBooksAsync(id),
            Reviews = book.Reviews.ToList(),
            ActiveBorrowRequest = activeBorrow,
            CanBorrow = book.TransactionType == TransactionType.Borrow && book.Status == BookStatus.Available && currentUserId != book.OwnerId,
            IsOwner = currentUserId == book.OwnerId,
            HasReviewed = hasReviewed,
            AvailableFrom = availableFrom
        };
    }

    public async Task<Book> CreateBookAsync(BookCreateViewModel model, string ownerId)
    {
        var book = new Book
        {
            Title = model.Title,
            Author = model.Author,
            ISBN = model.ISBN,
            Description = model.Description,
            Publisher = model.Publisher,
            PublishedYear = model.PublishedYear,
            Language = model.Language,
            Pages = model.Pages,
            Condition = model.Condition,
            TransactionType = model.TransactionType,
            Price = model.Price,
            DepositAmount = model.DepositAmount,
            BorrowDurationDays = model.BorrowDurationDays,
            City = model.City,
            CategoryId = model.CategoryId,
            OwnerId = ownerId
        };

        _context.Books.Add(book);
        await _context.SaveChangesAsync();

        if (model.Images != null && model.Images.Any())
        {
            bool isFirst = true;
            foreach (var img in model.Images.Take(5))
            {
                var path = await _imageService.SaveImageAsync(img, "books");
                _context.BookImages.Add(new BookImage { BookId = book.Id, ImagePath = path, IsPrimary = isFirst });
                isFirst = false;
            }
            await _context.SaveChangesAsync();
        }

        return book;
    }

    public async Task<bool> UpdateBookAsync(int id, BookCreateViewModel model, string userId)
    {
        var book = await _context.Books.FindAsync(id);
        if (book == null || book.OwnerId != userId) return false;

        book.Title = model.Title;
        book.Author = model.Author;
        book.ISBN = model.ISBN;
        book.Description = model.Description;
        book.Condition = model.Condition;
        book.TransactionType = model.TransactionType;
        book.Price = model.Price;
        book.DepositAmount = model.DepositAmount;
        book.BorrowDurationDays = model.BorrowDurationDays;
        book.City = model.City;
        book.CategoryId = model.CategoryId;
        book.UpdatedAt = DateTime.UtcNow;

        if (model.Images != null && model.Images.Any())
        {
            var oldImages = _context.BookImages.Where(bi => bi.BookId == id);
            foreach (var img in oldImages)
                await _imageService.DeleteImageAsync(img.ImagePath);
            _context.BookImages.RemoveRange(oldImages);

            bool isFirst = true;
            foreach (var img in model.Images.Take(5))
            {
                var path = await _imageService.SaveImageAsync(img, "books");
                _context.BookImages.Add(new BookImage { BookId = book.Id, ImagePath = path, IsPrimary = isFirst });
                isFirst = false;
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteBookAsync(int id, string userId)
    {
        var book = await _context.Books
            .Include(b => b.Images)
            .Include(b => b.BorrowRequests)
            .Include(b => b.Transactions)
            .Include(b => b.ExchangeRequests)
            .FirstOrDefaultAsync(b => b.Id == id);
            
        if (book == null || book.OwnerId != userId) return false;

        // Delete images from disk
        foreach (var img in book.Images)
            await _imageService.DeleteImageAsync(img.ImagePath);

        // Remove related borrow requests
        if (book.BorrowRequests.Any())
            _context.BorrowRequests.RemoveRange(book.BorrowRequests);

        // Remove related transactions
        if (book.Transactions.Any())
            _context.Transactions.RemoveRange(book.Transactions);

        // Remove related exchange requests (where it is either requested or offered)
        var relatedExchangeReqs = await _context.ExchangeRequests
            .Where(er => er.RequestedBookId == id || er.OfferedBookId == id)
            .ToListAsync();
        if (relatedExchangeReqs.Any())
            _context.ExchangeRequests.RemoveRange(relatedExchangeReqs);

        // Remove reviews related to this book
        var relatedReviews = await _context.Reviews.Where(r => r.BookId == id).ToListAsync();
        if (relatedReviews.Any())
            _context.Reviews.RemoveRange(relatedReviews);

        // Nullify BookId in conversations
        var conversations = await _context.Conversations.Where(c => c.BookId == id).ToListAsync();
        foreach (var conv in conversations)
            conv.BookId = null;

        // Nullify ReportedBookId in reports
        var reports = await _context.Reports.Where(r => r.ReportedBookId == id).ToListAsync();
        foreach (var rep in reports)
            rep.ReportedBookId = null;

        _context.Books.Remove(book);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<BookCardViewModel>> GetUserBooksAsync(string userId) =>
        (await _context.Books
            .Include(b => b.Category)
            .Include(b => b.Owner)
            .Include(b => b.Images)
            .Where(b => b.OwnerId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync())
        .Select(MapToCard).ToList();

    public async Task<List<BookCardViewModel>> GetFeaturedBooksAsync(int count = 6) =>
        (await _context.Books
            .Include(b => b.Category).Include(b => b.Owner).Include(b => b.Images)
            .Where(b => b.IsFeatured && b.Status == BookStatus.Available)
            .Take(count).ToListAsync()).Select(MapToCard).ToList();

    public async Task<List<BookCardViewModel>> GetTrendingBooksAsync(int count = 8) =>
        (await _context.Books
            .Include(b => b.Category).Include(b => b.Owner).Include(b => b.Images)
            .Where(b => b.Status == BookStatus.Available)
            .OrderByDescending(b => b.ViewCount)
            .Take(count).ToListAsync()).Select(MapToCard).ToList();

    public async Task<List<BookCardViewModel>> GetRecentBooksAsync(int count = 8) =>
        (await _context.Books
            .Include(b => b.Category).Include(b => b.Owner).Include(b => b.Images)
            .Where(b => b.Status == BookStatus.Available)
            .OrderByDescending(b => b.CreatedAt)
            .Take(count).ToListAsync()).Select(MapToCard).ToList();

    public async Task<List<BookCardViewModel>> GetSimilarBooksAsync(int bookId, int count = 4)
    {
        var book = await _context.Books.FindAsync(bookId);
        if (book == null) return new();

        return (await _context.Books
            .Include(b => b.Category).Include(b => b.Owner).Include(b => b.Images)
            .Where(b => b.CategoryId == book.CategoryId && b.Id != bookId && b.Status == BookStatus.Available)
            .Take(count).ToListAsync()).Select(MapToCard).ToList();
    }

    public async Task IncrementViewCountAsync(int bookId)
    {
        var book = await _context.Books.FindAsync(bookId);
        if (book != null) { book.ViewCount++; await _context.SaveChangesAsync(); }
    }

    private BookCardViewModel MapToCard(Book b) => new()
    {
        Id = b.Id,
        Title = b.Title,
        Author = b.Author,
        PrimaryImage = b.Images.FirstOrDefault(i => i.IsPrimary)?.ImagePath ?? b.Images.FirstOrDefault()?.ImagePath,
        Condition = b.Condition,
        TransactionType = b.TransactionType,
        Status = b.Status,
        Price = b.Price,
        City = b.City,
        CategoryName = b.Category?.Name ?? "",
        OwnerName = b.Owner?.FullName ?? "",
        OwnerPicture = b.Owner?.ProfilePicture,
        OwnerRating = b.Owner?.AverageRating ?? 0,
        CreatedAt = b.CreatedAt,
        ViewCount = b.ViewCount,
        BorrowDurationDays = b.BorrowDurationDays
    };
}
