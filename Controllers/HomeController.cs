using BookBridge.Data;
using BookBridge.Models.Entities;
using BookBridge.Models.ViewModels;
using BookBridge.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookBridge.Controllers;

public class HomeController : Controller
{
    private readonly IBookService _bookService;
    private readonly ApplicationDbContext _context;

    public HomeController(IBookService bookService, ApplicationDbContext context)
    {
        _bookService = bookService;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var vm = new HomeViewModel
        {
            FeaturedBooks = await _bookService.GetFeaturedBooksAsync(6),
            RecentBooks = await _bookService.GetRecentBooksAsync(8),
            TrendingBooks = await _bookService.GetTrendingBooksAsync(8),
            Categories = await _context.Categories.ToListAsync(),
            TotalBooks = await _context.Books.CountAsync(),
            TotalUsers = await _context.Users.CountAsync(),
            TotalTransactions = await _context.Transactions.CountAsync(),
            BooksAvailableForFree = await _context.Books.CountAsync(b =>
                (b.TransactionType == TransactionType.Donate || b.TransactionType == TransactionType.Borrow)
                && b.Status == BookStatus.Available)
        };
        return View(vm);
    }

    public IActionResult About() => View();
    public IActionResult Contact() => View();
    public IActionResult FAQ() => View();
    public IActionResult Privacy() => View();
    public IActionResult Terms() => View();

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SubscribeNewsletter(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains("@"))
            return Json(new { success = false, message = "Please enter a valid email address." });

        var exists = await _context.NewsletterSubscriptions.AnyAsync(s => s.Email == email);
        if (exists)
            return Json(new { success = true, message = "You are already subscribed to our newsletter!" });

        _context.NewsletterSubscriptions.Add(new NewsletterSubscription { Email = email });
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Successfully subscribed! Stay tuned for BookBridge updates." });
    }

    [HttpPost]
    public IActionResult Contact(string name, string email, string message)
    {
        TempData["Success"] = "Message sent successfully! We'll get back to you soon.";
        return RedirectToAction(nameof(Contact));
    }
}
