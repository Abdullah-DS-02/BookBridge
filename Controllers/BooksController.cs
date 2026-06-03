using BookBridge.Data;
using BookBridge.Models.Entities;
using BookBridge.Models.ViewModels;
using BookBridge.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookBridge.Controllers;

public class BooksController : Controller
{
    private readonly IBookService _bookService;
    private readonly IBorrowService _borrowService;
    private readonly IReviewService _reviewService;
    private readonly IChatService _chatService;
    private readonly INotificationService _notificationService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public BooksController(IBookService bookService, IBorrowService borrowService,
        IReviewService reviewService, IChatService chatService,
        INotificationService notificationService, ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _bookService = bookService;
        _borrowService = borrowService;
        _reviewService = reviewService;
        _chatService = chatService;
        _notificationService = notificationService;
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(BookListViewModel filters)
    {
        var vm = await _bookService.GetBooksAsync(filters);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var currentUserId = _userManager.GetUserId(User);
        var vm = await _bookService.GetBookDetailsAsync(id, currentUserId);
        await _bookService.IncrementViewCountAsync(id);
        return View(vm);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new BookCreateViewModel
        {
            Categories = await _context.Categories.ToListAsync()
        };
        return View(vm);
    }

    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = await _context.Categories.ToListAsync();
            return View(model);
        }

        var userId = _userManager.GetUserId(User)!;
        var book = await _bookService.CreateBookAsync(model, userId);
        TempData["Success"] = "Book listed successfully!";
        return RedirectToAction(nameof(Details), new { id = book.Id });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var book = await _bookService.GetBookByIdAsync(id);
        if (book == null || book.OwnerId != userId) return Forbid();

        var vm = new BookCreateViewModel
        {
            Title = book.Title, Author = book.Author, ISBN = book.ISBN,
            Description = book.Description, Publisher = book.Publisher,
            PublishedYear = book.PublishedYear, Language = book.Language,
            Pages = book.Pages, Condition = book.Condition,
            TransactionType = book.TransactionType, Price = book.Price,
            DepositAmount = book.DepositAmount, BorrowDurationDays = book.BorrowDurationDays,
            City = book.City, CategoryId = book.CategoryId,
            Categories = await _context.Categories.ToListAsync()
        };
        ViewBag.BookId = id;
        return View(vm);
    }

    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BookCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = await _context.Categories.ToListAsync();
            return View(model);
        }

        var userId = _userManager.GetUserId(User)!;
        var success = await _bookService.UpdateBookAsync(id, model, userId);
        if (!success) return Forbid();

        TempData["Success"] = "Book updated successfully!";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        await _bookService.DeleteBookAsync(id, userId);
        TempData["Success"] = "Book removed successfully.";
        return RedirectToAction("MyBooks", "Dashboard");
    }

    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BorrowRequest(BorrowRequestViewModel model)
    {
        var userId = _userManager.GetUserId(User)!;
        var success = await _borrowService.CreateBorrowRequestAsync(model, userId);
        if (success)
            TempData["Success"] = "Borrow request sent! Waiting for owner approval.";
        else
            TempData["Error"] = "Could not send borrow request. Book may be unavailable.";
        return RedirectToAction(nameof(Details), new { id = model.BookId });
    }

    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveBorrow(int requestId)
    {
        var userId = _userManager.GetUserId(User)!;
        await _borrowService.ApproveBorrowRequestAsync(requestId, userId);
        TempData["Success"] = "Borrow request approved!";
        return RedirectToAction("Requests", "Dashboard");
    }

    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectBorrow(int requestId, string reason)
    {
        var userId = _userManager.GetUserId(User)!;
        await _borrowService.RejectBorrowRequestAsync(requestId, userId, reason);
        TempData["Success"] = "Request rejected.";
        return RedirectToAction("Requests", "Dashboard");
    }

    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReturnBook(int requestId)
    {
        var userId = _userManager.GetUserId(User)!;
        await _borrowService.ReturnBookAsync(requestId, userId);
        TempData["Success"] = "Book marked as returned!";
        return RedirectToAction("BorrowedBooks", "Dashboard");
    }

    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitReview(CreateReviewViewModel model)
    {
        var userId = _userManager.GetUserId(User)!;
        await _reviewService.CreateReviewAsync(model, userId);
        TempData["Success"] = "Review submitted successfully!";
        return RedirectToAction(nameof(Details), new { id = model.BookId });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> StartChat(string sellerId, int bookId)
    {
        var userId = _userManager.GetUserId(User)!;
        if (userId == sellerId) return BadRequest();
        var conv = await _chatService.GetOrCreateConversationAsync(userId, sellerId, bookId);
        return RedirectToAction("Index", "Chat", new { conversationId = conv.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Search(string q)
    {
        var filters = new BookListViewModel { SearchQuery = q };
        var vm = await _bookService.GetBooksAsync(filters);
        return View("Index", vm);
    }
}
