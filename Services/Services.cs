using BookBridge.Data;
using BookBridge.Models.Entities;
using BookBridge.Models.ViewModels;
using BookBridge.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace BookBridge.Services;

public class ImageService : IImageService
{
    private readonly IWebHostEnvironment _env;

    public ImageService(IWebHostEnvironment env) => _env = env;

    public async Task<string> SaveImageAsync(IFormFile file, string folder)
    {
        if (file == null || file.Length == 0) return GetDefaultImagePath();

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", folder);
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.FileName).ToLower();
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        if (imageExtensions.Contains(ext))
        {
            try
            {
                using var image = await Image.LoadAsync(file.OpenReadStream());
                image.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(800, 600) }));
                await image.SaveAsync(filePath);
                return $"/uploads/{folder}/{fileName}";
            }
            catch
            {
                // Fallback to raw copy if ImageSharp fails
            }
        }

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/{folder}/{fileName}";
    }

    public async Task<List<string>> SaveImagesAsync(List<IFormFile> files, string folder)
    {
        var paths = new List<string>();
        foreach (var file in files)
            paths.Add(await SaveImageAsync(file, folder));
        return paths;
    }

    public Task DeleteImageAsync(string imagePath)
    {
        if (!string.IsNullOrEmpty(imagePath))
        {
            var fullPath = Path.Combine(_env.WebRootPath, imagePath.TrimStart('/'));
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
        return Task.CompletedTask;
    }

    public string GetDefaultImagePath() => "/images/book-placeholder.png";
}

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context) => _context = context;

    public async Task CreateNotificationAsync(string userId, string title, string body, string type = "info", string? link = null)
    {
        _context.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Body = body,
            Type = type,
            Link = link
        });
        await _context.SaveChangesAsync();
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(string userId, int count = 20) =>
        await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(count).ToListAsync();

    public async Task<int> GetUnreadCountAsync(string userId) =>
        await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task MarkAsReadAsync(int notificationId, string userId)
    {
        var n = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        if (n != null) { n.IsRead = true; await _context.SaveChangesAsync(); }
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        var notifications = await _context.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
        notifications.ForEach(n => n.IsRead = true);
        await _context.SaveChangesAsync();
    }
}

public class BorrowService : IBorrowService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public BorrowService(ApplicationDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<bool> CreateBorrowRequestAsync(BorrowRequestViewModel model, string borrowerId)
    {
        var book = await _context.Books.FindAsync(model.BookId);
        if (book == null || book.Status != BookStatus.Available) return false;
        if (book.OwnerId == borrowerId) return false;

        var existing = await _context.BorrowRequests
            .AnyAsync(br => br.BookId == model.BookId && br.BorrowerId == borrowerId && br.Status == RequestStatus.Pending);
        if (existing) return false;

        var request = new BorrowRequest
        {
            BookId = model.BookId,
            BorrowerId = borrowerId,
            OwnerId = book.OwnerId,
            BorrowDays = model.BorrowDays,
            Message = model.Message
        };

        _context.BorrowRequests.Add(request);
        await _context.SaveChangesAsync();

        await _notificationService.CreateNotificationAsync(book.OwnerId, "New Borrow Request",
            $"Someone wants to borrow '{book.Title}'", "borrow", $"/Dashboard/Requests");

        return true;
    }

    public async Task<bool> ApproveBorrowRequestAsync(int requestId, string ownerId)
    {
        var request = await _context.BorrowRequests
            .Include(br => br.Book).Include(br => br.Borrower)
            .FirstOrDefaultAsync(br => br.Id == requestId && br.OwnerId == ownerId);

        if (request == null || request.Status != RequestStatus.Pending) return false;

        request.Status = RequestStatus.Approved;
        request.ApprovedAt = DateTime.UtcNow;
        request.BorrowedAt = DateTime.UtcNow;
        request.DueDate = DateTime.UtcNow.AddDays(request.BorrowDays);
        request.Book.Status = BookStatus.Borrowed;

        await _context.SaveChangesAsync();

        await _notificationService.CreateNotificationAsync(request.BorrowerId, "Borrow Request Approved!",
            $"Your request for '{request.Book.Title}' has been approved. Due: {request.DueDate:dd MMM yyyy}", "success");

        return true;
    }

    public async Task<bool> RejectBorrowRequestAsync(int requestId, string ownerId, string reason)
    {
        var request = await _context.BorrowRequests
            .Include(br => br.Book)
            .FirstOrDefaultAsync(br => br.Id == requestId && br.OwnerId == ownerId);

        if (request == null) return false;

        request.Status = RequestStatus.Rejected;
        request.RejectionReason = reason;
        await _context.SaveChangesAsync();

        await _notificationService.CreateNotificationAsync(request.BorrowerId, "Borrow Request Rejected",
            $"Your request for '{request.Book.Title}' was declined.", "warning");

        return true;
    }

    public async Task<bool> ReturnBookAsync(int requestId, string borrowerId)
    {
        var request = await _context.BorrowRequests
            .Include(br => br.Book)
            .FirstOrDefaultAsync(br => br.Id == requestId && br.BorrowerId == borrowerId);

        if (request == null || request.Status != RequestStatus.Approved) return false;

        request.Status = RequestStatus.Completed;
        request.ReturnedAt = DateTime.UtcNow;
        request.Book.Status = BookStatus.Available;

        await _context.SaveChangesAsync();

        await _notificationService.CreateNotificationAsync(request.OwnerId, "Book Returned",
            $"'{request.Book.Title}' has been returned.", "info");

        return true;
    }

    public async Task<List<BorrowRequest>> GetUserBorrowRequestsAsync(string userId) =>
        await _context.BorrowRequests
            .Include(br => br.Book).ThenInclude(b => b.Images)
            .Include(br => br.Owner)
            .Include(br => br.Borrower)
            .Where(br => br.BorrowerId == userId || br.OwnerId == userId)
            .OrderByDescending(br => br.RequestedAt)
            .ToListAsync();

    public async Task<List<BorrowRequest>> GetBookBorrowRequestsAsync(int bookId, string ownerId) =>
        await _context.BorrowRequests
            .Include(br => br.Borrower)
            .Where(br => br.BookId == bookId && br.OwnerId == ownerId)
            .ToListAsync();

    public async Task CheckOverdueBorrowsAsync()
    {
        var overdue = await _context.BorrowRequests
            .Include(br => br.Book)
            .Where(br => br.Status == RequestStatus.Approved && br.DueDate < DateTime.UtcNow && !br.IsLate)
            .ToListAsync();

        foreach (var request in overdue)
        {
            request.IsLate = true;
            await _context.SaveChangesAsync();
            await _notificationService.CreateNotificationAsync(request.BorrowerId, "⚠️ Book Overdue!",
                $"'{request.Book.Title}' was due on {request.DueDate:dd MMM}. Please return it immediately!", "danger");
            await _notificationService.CreateNotificationAsync(request.OwnerId, "Book Overdue",
                $"'{request.Book.Title}' is overdue from a borrower.", "warning");
        }
    }
}

public class ChatService : IChatService
{
    private readonly ApplicationDbContext _context;
    private readonly IImageService _imageService;

    public ChatService(ApplicationDbContext context, IImageService imageService)
    {
        _context = context;
        _imageService = imageService;
    }

    public async Task<Conversation> GetOrCreateConversationAsync(string user1Id, string user2Id, int? bookId = null)
    {
        var existing = await _context.Conversations
            .FirstOrDefaultAsync(c =>
                (c.User1Id == user1Id && c.User2Id == user2Id) ||
                (c.User1Id == user2Id && c.User2Id == user1Id));

        if (existing != null) return existing;

        var conv = new Conversation { User1Id = user1Id, User2Id = user2Id, BookId = bookId };
        _context.Conversations.Add(conv);
        await _context.SaveChangesAsync();
        return conv;
    }

    public async Task<ChatViewModel> GetChatViewModelAsync(int conversationId, string userId)
    {
        var conv = await _context.Conversations
            .Include(c => c.User1)
            .Include(c => c.User2)
            .Include(c => c.Messages).ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(c => c.Id == conversationId) ?? throw new Exception("Conversation not found");

        var otherUser = conv.User1Id == userId ? conv.User2 : conv.User1;
        var allConvs = await GetUserConversationsAsync(userId);

        return new ChatViewModel
        {
            Conversation = conv,
            Messages = conv.Messages.OrderBy(m => m.SentAt).ToList(),
            OtherUser = otherUser,
            AllConversations = allConvs
        };
    }

    public async Task<Message> SendMessageAsync(int conversationId, string senderId, string content, IFormFile? image = null)
    {
        string? imagePath = null;
        if (image != null)
            imagePath = await _imageService.SaveImageAsync(image, "chat");

        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Content = content,
            ImagePath = imagePath
        };

        _context.Messages.Add(message);

        var conv = await _context.Conversations.FindAsync(conversationId);
        if (conv != null) conv.LastMessageAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return message;
    }

    public async Task<List<ConversationViewModel>> GetUserConversationsAsync(string userId)
    {
        var conversations = await _context.Conversations
            .Include(c => c.User1)
            .Include(c => c.User2)
            .Include(c => c.Messages)
            .Include(c => c.Book)
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync();

        return conversations.Select(c =>
        {
            var otherUser = c.User1Id == userId ? c.User2 : c.User1;
            var lastMsg = c.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault();
            return new ConversationViewModel
            {
                ConversationId = c.Id,
                OtherUserId = otherUser.Id,
                OtherUserName = otherUser.FullName,
                OtherUserPicture = otherUser.ProfilePicture,
                LastMessage = lastMsg?.Content,
                LastMessageAt = lastMsg?.SentAt,
                UnreadCount = c.Messages.Count(m => !m.IsRead && m.SenderId != userId),
                BookTitle = c.Book?.Title
            };
        }).ToList();
    }

    public async Task MarkMessagesAsReadAsync(int conversationId, string userId)
    {
        var unread = await _context.Messages
            .Where(m => m.ConversationId == conversationId && m.SenderId != userId && !m.IsRead)
            .ToListAsync();
        unread.ForEach(m => m.IsRead = true);
        await _context.SaveChangesAsync();
    }
}

public class ReviewService : IReviewService
{
    private readonly ApplicationDbContext _context;

    public ReviewService(ApplicationDbContext context) => _context = context;

    public async Task<Review> CreateReviewAsync(CreateReviewViewModel model, string reviewerId)
    {
        var review = new Review
        {
            Rating = model.Rating,
            Comment = model.Comment,
            ReviewerId = reviewerId,
            ReviewedUserId = model.ReviewedUserId,
            BookId = model.BookId
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        // Update user average rating
        var user = await _context.Users.FindAsync(model.ReviewedUserId);
        if (user != null)
        {
            var ratings = await _context.Reviews
                .Where(r => r.ReviewedUserId == model.ReviewedUserId)
                .Select(r => r.Rating).ToListAsync();
            user.AverageRating = ratings.Average();
            user.TotalRatings = ratings.Count;
            await _context.SaveChangesAsync();
        }

        return review;
    }

    public async Task<List<Review>> GetUserReviewsAsync(string userId) =>
        await _context.Reviews
            .Include(r => r.Reviewer)
            .Include(r => r.Book)
            .Where(r => r.ReviewedUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task<List<Review>> GetBookReviewsAsync(int bookId) =>
        await _context.Reviews
            .Include(r => r.Reviewer)
            .Where(r => r.BookId == bookId)
            .ToListAsync();

    public async Task<bool> HasUserReviewedAsync(string reviewerId, string reviewedUserId) =>
        await _context.Reviews.AnyAsync(r => r.ReviewerId == reviewerId && r.ReviewedUserId == reviewedUserId);
}

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IImageService _imageService;
    private readonly IBookService _bookService;
    private readonly INotificationService _notificationService;

    public UserService(ApplicationDbContext context, IImageService imageService,
        IBookService bookService, INotificationService notificationService)
    {
        _context = context;
        _imageService = imageService;
        _bookService = bookService;
        _notificationService = notificationService;
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(string userId) =>
        await _context.Users.FindAsync(userId);

    public async Task<ProfileViewModel> GetProfileAsync(string userId, string? currentUserId)
    {
        var user = await _context.Users.FindAsync(userId) ?? throw new Exception("User not found");
        var books = await _bookService.GetUserBooksAsync(userId);
        var reviews = await _context.Reviews
            .Include(r => r.Reviewer)
            .Where(r => r.ReviewedUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return new ProfileViewModel
        {
            User = user,
            Books = books,
            Reviews = reviews,
            IsOwnProfile = userId == currentUserId
        };
    }

    public async Task<bool> UpdateProfileAsync(string userId, EditProfileViewModel model)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.FullName = model.FullName;
        user.PhoneNumber = model.PhoneNumber;
        user.Address = model.Address;
        user.City = model.City;
        user.CNIC = model.CNIC;

        if (model.ProfilePicture != null)
        {
            if (!string.IsNullOrEmpty(user.ProfilePicture))
                await _imageService.DeleteImageAsync(user.ProfilePicture);
            user.ProfilePicture = await _imageService.SaveImageAsync(model.ProfilePicture, "profiles");
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateProfilePictureAsync(string userId, IFormFile image)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;
        
        if (!string.IsNullOrEmpty(user.ProfilePicture))
            await _imageService.DeleteImageAsync(user.ProfilePicture);
            
        user.ProfilePicture = await _imageService.SaveImageAsync(image, "profiles");
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<DashboardViewModel> GetDashboardAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId) ?? throw new Exception("User not found");
        var myBooks = await _bookService.GetUserBooksAsync(userId);
        var requests = await _context.BorrowRequests
            .Include(br => br.Book).Include(br => br.Borrower)
            .Where(br => br.OwnerId == userId)
            .OrderByDescending(br => br.RequestedAt)
            .Take(5).ToListAsync();
        var notifications = await _notificationService.GetUserNotificationsAsync(userId, 5);

        return new DashboardViewModel
        {
            User = user,
            TotalBooksListed = myBooks.Count,
            ActiveBorrows = await _context.BorrowRequests.CountAsync(br => br.OwnerId == userId && br.Status == RequestStatus.Approved),
            PendingRequests = await _context.BorrowRequests.CountAsync(br => br.OwnerId == userId && br.Status == RequestStatus.Pending),
            TotalTransactions = await _context.Transactions.CountAsync(t => t.SellerId == userId),
            TotalEarnings = await _context.Transactions.Where(t => t.SellerId == userId && t.Status == TransactionStatus.Completed).SumAsync(t => t.Amount),
            MyBooks = myBooks.Take(6).ToList(),
            RecentRequests = requests,
            Notifications = notifications
        };
    }

    public async Task<bool> BanUserAsync(string userId, string reason)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;
        user.IsBanned = true;
        user.BanReason = reason;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnbanUserAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;
        user.IsBanned = false;
        user.BanReason = null;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ApplicationUser>> GetAllUsersAsync() =>
        await _context.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();
}

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _context;

    public AdminService(ApplicationDbContext context) => _context = context;

    public async Task<AdminDashboardViewModel> GetDashboardAsync() => new()
    {
        TotalUsers = await _context.Users.CountAsync(),
        TotalBooks = await _context.Books.CountAsync(),
        TotalTransactions = await _context.Transactions.CountAsync(),
        PendingTickets = await _context.SupportTickets.CountAsync(t => t.Status == "Open"),
        BannedUsers = await _context.Users.CountAsync(u => u.IsBanned),
        PendingReports = await _context.Reports.CountAsync(r => r.Status == "Pending"),
        TotalRevenue = await _context.Transactions.Where(t => t.Status == TransactionStatus.Completed).SumAsync(t => t.Amount),
        RecentUsers = await _context.Users.OrderByDescending(u => u.CreatedAt).Take(5).ToListAsync(),
        RecentBooks = await _context.Books.Include(b => b.Owner).OrderByDescending(b => b.CreatedAt).Take(5).ToListAsync(),
        OpenTickets = await _context.SupportTickets.Include(t => t.User).Where(t => t.Status == "Open").Take(5).ToListAsync(),
        PendingReportsList = await _context.Reports.Include(r => r.Reporter).Where(r => r.Status == "Pending").Take(5).ToListAsync()
    };

    public async Task<bool> FeatureBookAsync(int bookId)
    {
        var book = await _context.Books.FindAsync(bookId);
        if (book == null) return false;
        book.IsFeatured = !book.IsFeatured;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveBookAsync(int bookId)
    {
        var book = await _context.Books.FindAsync(bookId);
        if (book == null) return false;
        _context.Books.Remove(book);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Report>> GetPendingReportsAsync() =>
        await _context.Reports.Include(r => r.Reporter).Where(r => r.Status == "Pending").ToListAsync();

    public async Task<bool> ResolveReportAsync(int reportId, string action)
    {
        var report = await _context.Reports.FindAsync(reportId);
        if (report == null) return false;
        report.Status = action;
        await _context.SaveChangesAsync();
        return true;
    }
}

public class SupportService : ISupportService
{
    private readonly ApplicationDbContext _context;

    public SupportService(ApplicationDbContext context) => _context = context;

    public async Task<SupportTicket> CreateTicketAsync(string userId, string subject, string description)
    {
        var ticket = new SupportTicket { UserId = userId, Subject = subject, Description = description };
        _context.SupportTickets.Add(ticket);
        await _context.SaveChangesAsync();
        return ticket;
    }

    public async Task<List<SupportTicket>> GetUserTicketsAsync(string userId) =>
        await _context.SupportTickets.Where(t => t.UserId == userId).OrderByDescending(t => t.CreatedAt).ToListAsync();

    public async Task<List<SupportTicket>> GetAllTicketsAsync() =>
        await _context.SupportTickets.Include(t => t.User).OrderByDescending(t => t.CreatedAt).ToListAsync();

    public async Task<bool> ReplyToTicketAsync(int ticketId, string adminReply)
    {
        var ticket = await _context.SupportTickets.FindAsync(ticketId);
        if (ticket == null) return false;
        ticket.AdminReply = adminReply;
        ticket.Status = "Replied";
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CloseTicketAsync(int ticketId)
    {
        var ticket = await _context.SupportTickets.FindAsync(ticketId);
        if (ticket == null) return false;
        ticket.Status = "Closed";
        ticket.ResolvedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
}
