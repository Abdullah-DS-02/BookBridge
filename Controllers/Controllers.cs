using BookBridge.Data;
using BookBridge.Hubs;
using BookBridge.Models.Entities;
using BookBridge.Models.ViewModels;
using BookBridge.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;

namespace BookBridge.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IUserService _userService;
    private readonly IBookService _bookService;
    private readonly IBorrowService _borrowService;
    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public DashboardController(IUserService userService, IBookService bookService,
        IBorrowService borrowService, INotificationService notificationService,
        UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userService = userService;
        _bookService = bookService;
        _borrowService = borrowService;
        _notificationService = notificationService;
        _userManager = userManager;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var vm = await _userService.GetDashboardAsync(userId);
        return View(vm);
    }

    public async Task<IActionResult> MyBooks()
    {
        var userId = _userManager.GetUserId(User)!;
        var books = await _bookService.GetUserBooksAsync(userId);
        return View(books);
    }

    public async Task<IActionResult> Requests()
    {
        var userId = _userManager.GetUserId(User)!;
        var requests = await _borrowService.GetUserBorrowRequestsAsync(userId);
        return View(requests);
    }

    public async Task<IActionResult> BorrowedBooks()
    {
        var userId = _userManager.GetUserId(User)!;
        var requests = await _context.BorrowRequests
            .Include(br => br.Book).ThenInclude(b => b.Images)
            .Include(br => br.Owner)
            .Where(br => br.BorrowerId == userId && br.Status == RequestStatus.Approved)
            .ToListAsync();
        return View(requests);
    }

    public async Task<IActionResult> Notifications()
    {
        var userId = _userManager.GetUserId(User)!;
        var notifications = await _notificationService.GetUserNotificationsAsync(userId, 50);
        await _notificationService.MarkAllAsReadAsync(userId);
        return View(notifications);
    }



    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = _userManager.GetUserId(User)!;
        var vm = await _userService.GetProfileAsync(userId, userId);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();
        var vm = new EditProfileViewModel
        {
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            City = user.City,
            CNIC = user.CNIC,
            CurrentProfilePicture = user.ProfilePicture
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(EditProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null) model.CurrentProfilePicture = user.ProfilePicture;
            return View(model);
        }
        var userId = _userManager.GetUserId(User)!;
        await _userService.UpdateProfileAsync(userId, model);
        TempData["Success"] = "Profile updated successfully!";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
    {
        if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
            return Json(new { success = false, message = "Both current and new passwords are required." });

        if (newPassword.Length < 8)
            return Json(new { success = false, message = "New password must be at least 8 characters long." });

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Json(new { success = false, message = "User not found." });

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (result.Succeeded)
        {
            return Json(new { success = true, message = "Password updated successfully!" });
        }

        return Json(new { success = false, message = string.Join(" ", result.Errors.Select(e => e.Description)) });
    }

    [HttpPost]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return Json(new { success = false, message = "No file selected." });

        var userId = _userManager.GetUserId(User)!;
        var success = await _userService.UpdateProfilePictureAsync(userId, file);
        if (success)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            return Json(new { success = true, message = "Profile picture updated successfully!", imagePath = user?.ProfilePicture });
        }

        return Json(new { success = false, message = "Upload failed. Please try again." });
    }

    [HttpGet]
    public async Task<IActionResult> PublicProfile(string id)
    {
        var currentUserId = _userManager.GetUserId(User);
        var vm = await _userService.GetProfileAsync(id, currentUserId);
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> MarkNotificationRead(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        await _notificationService.MarkAsReadAsync(id, userId);
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = _userManager.GetUserId(User)!;
        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Json(new { count });
    }

    public async Task<IActionResult> Support()
    {
        var userId = _userManager.GetUserId(User)!;
        var tickets = await _context.SupportTickets
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
        return View(tickets);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTicket(string subject, string description)
    {
        var userId = _userManager.GetUserId(User)!;
        _context.SupportTickets.Add(new SupportTicket { UserId = userId, Subject = subject, Description = description });
        await _context.SaveChangesAsync();
        TempData["Success"] = "Support ticket submitted!";
        return RedirectToAction(nameof(Support));
    }
}

[Authorize]
public class ChatController : Controller
{
    private readonly IChatService _chatService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly Microsoft.AspNetCore.SignalR.IHubContext<ChatHub> _hubContext;

    public ChatController(IChatService chatService, UserManager<ApplicationUser> userManager, Microsoft.AspNetCore.SignalR.IHubContext<ChatHub> hubContext)
    {
        _chatService = chatService;
        _userManager = userManager;
        _hubContext = hubContext;
    }

    public async Task<IActionResult> Index(int? conversationId)
    {
        var userId = _userManager.GetUserId(User)!;
        var conversations = await _chatService.GetUserConversationsAsync(userId);

        if (conversationId == null && conversations.Any())
            conversationId = conversations.First().ConversationId;

        if (conversationId == null)
            return View(new ChatViewModel { AllConversations = conversations });

        var vm = await _chatService.GetChatViewModelAsync(conversationId.Value, userId);
        vm.AllConversations = conversations;
        return View(vm);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SendMessage(int conversationId, string? content, IFormFile? file)
    {
        var userId = _userManager.GetUserId(User)!;
        var msg = await _chatService.SendMessageAsync(conversationId, userId, content ?? "", file);

        await _hubContext.Clients.Group($"conv_{conversationId}").SendAsync("ReceiveMessage", new
        {
            id = msg.Id,
            content = msg.Content,
            senderId = msg.SenderId,
            sentAt = msg.SentAt.ToString("HH:mm"),
            imagePath = msg.ImagePath
        });

        return Json(new { success = true, messageId = msg.Id, sentAt = msg.SentAt, imagePath = msg.ImagePath });
    }

    [HttpPost]
    public async Task<IActionResult> StartConversation(string userId, int? bookId)
    {
        var currentUserId = _userManager.GetUserId(User)!;
        var conv = await _chatService.GetOrCreateConversationAsync(currentUserId, userId, bookId);
        return RedirectToAction(nameof(Index), new { conversationId = conv.Id });
    }
}

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IAdminService _adminService;
    private readonly IUserService _userService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly INotificationService _notificationService;

    public AdminController(IAdminService adminService, IUserService userService,
        ApplicationDbContext context, UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager, INotificationService notificationService)
    {
        _adminService = adminService;
        _userService = userService;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _notificationService = notificationService;
    }

    public async Task<IActionResult> Index()
    {
        var vm = await _adminService.GetDashboardAsync();
        return View(vm);
    }

    public async Task<IActionResult> Users()
    {
        var users = await _userService.GetAllUsersAsync();
        return View(users);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BanUser(string id, string reason)
    {
        await _userService.BanUserAsync(id, reason);
        TempData["Success"] = "User banned.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UnbanUser(string id)
    {
        await _userService.UnbanUserAsync(id);
        TempData["Success"] = "User unbanned.";
        return RedirectToAction(nameof(Users));
    }

    public async Task<IActionResult> Books()
    {
        var books = await _context.Books
            .Include(b => b.Owner).Include(b => b.Category).Include(b => b.Images)
            .OrderByDescending(b => b.CreatedAt).ToListAsync();
        return View(books);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> FeatureBook(int id)
    {
        await _adminService.FeatureBookAsync(id);
        TempData["Success"] = "Book featured status toggled.";
        return RedirectToAction(nameof(Books));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveBook(int id)
    {
        await _adminService.RemoveBookAsync(id);
        TempData["Success"] = "Book removed.";
        return RedirectToAction(nameof(Books));
    }

    public async Task<IActionResult> Reports()
    {
        var reports = await _context.Reports
            .Include(r => r.Reporter)
            .OrderByDescending(r => r.CreatedAt).ToListAsync();
        return View(reports);
    }

    public async Task<IActionResult> Tickets()
    {
        var tickets = await _context.SupportTickets
            .Include(t => t.User)
            .OrderByDescending(t => t.CreatedAt).ToListAsync();
        return View(tickets);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplyTicket(int id, string reply)
    {
        var ticket = await _context.SupportTickets.FindAsync(id);
        if (ticket != null) { ticket.AdminReply = reply; ticket.Status = "Replied"; await _context.SaveChangesAsync(); }
        TempData["Success"] = "Reply sent.";
        return RedirectToAction(nameof(Tickets));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MakeAdmin(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user != null)
        {
            if (!await _roleManager.RoleExistsAsync("Admin"))
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            await _userManager.AddToRoleAsync(user, "Admin");
        }
        TempData["Success"] = "User promoted to Admin.";
        return RedirectToAction(nameof(Users));
    }



    public async Task<IActionResult> Subscribers()
    {
        var subscribers = await _context.NewsletterSubscriptions
            .OrderByDescending(s => s.SubscribedAt)
            .ToListAsync();
        return View(subscribers);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SendAnnouncement(string title, string message)
    {
        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(message))
        {
            TempData["Error"] = "Title and message are required.";
            return RedirectToAction(nameof(Subscribers));
        }

        var subscribedEmails = await _context.NewsletterSubscriptions
            .Select(s => s.Email)
            .ToListAsync();

        if (!subscribedEmails.Any())
        {
            TempData["Error"] = "No subscribers found to send announcement to.";
            return RedirectToAction(nameof(Subscribers));
        }

        var targetUsers = await _userManager.Users
            .Where(u => u.Email != null && subscribedEmails.Contains(u.Email))
            .ToListAsync();

        if (!targetUsers.Any())
        {
            TempData["Success"] = "Subscribers list loaded, but no matching registered users found to notify.";
            return RedirectToAction(nameof(Subscribers));
        }

        int count = 0;
        foreach (var user in targetUsers)
        {
            await _notificationService.CreateNotificationAsync(
                user.Id,
                title,
                message,
                "announcement",
                "/Dashboard/Notifications"
            );
            count++;
        }

        TempData["Success"] = $"Announcement successfully sent to {count} matching subscribers.";
        return RedirectToAction(nameof(Subscribers));
    }

    public async Task<IActionResult> Categories()
    {
        var categories = await _context.Categories.ToListAsync();
        return View(categories);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(string name, string description, string icon)
    {
        _context.Categories.Add(new Category { Name = name, Description = description, Icon = icon });
        await _context.SaveChangesAsync();
        TempData["Success"] = "Category added.";
        return RedirectToAction(nameof(Categories));
    }
}
