using BookBridge.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace BookBridge.Models.ViewModels;

// Auth ViewModels
public class RegisterViewModel
{
    [Required] public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(8)] public string Password { get; set; } = string.Empty;
    [Compare("Password")] public string ConfirmPassword { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? City { get; set; }
    public bool AgreeToTerms { get; set; }
}

public class LoginViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}

public class ForgotPasswordViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    [Required] public string Token { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(8)] public string NewPassword { get; set; } = string.Empty;
    [Compare("NewPassword")] public string ConfirmPassword { get; set; } = string.Empty;
}

// Book ViewModels
public class BookCreateViewModel
{
    [Required] public string Title { get; set; } = string.Empty;
    [Required] public string Author { get; set; } = string.Empty;
    public string? ISBN { get; set; }
    public string? Description { get; set; }
    public string? Publisher { get; set; }
    public int? PublishedYear { get; set; }
    public string Language { get; set; } = "English";
    public int? Pages { get; set; }
    [Required] public BookCondition Condition { get; set; }
    [Required] public TransactionType TransactionType { get; set; }
    public decimal? Price { get; set; }
    public decimal? DepositAmount { get; set; }
    public int? BorrowDurationDays { get; set; }
    public string? City { get; set; }
    [Required] public int CategoryId { get; set; }
    public List<IFormFile>? Images { get; set; }
    public List<Category> Categories { get; set; } = new();
}

public class BookListViewModel
{
    public IEnumerable<BookCardViewModel> Books { get; set; } = new List<BookCardViewModel>();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public string? SearchQuery { get; set; }
    public string? Category { get; set; }
    public string? TransactionType { get; set; }
    public string? Condition { get; set; }
    public string? SortBy { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? City { get; set; }
    public List<Category> Categories { get; set; } = new();
}

public class BookCardViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? PrimaryImage { get; set; }
    public BookCondition Condition { get; set; }
    public TransactionType TransactionType { get; set; }
    public BookStatus Status { get; set; }
    public decimal? Price { get; set; }
    public string? City { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string? OwnerPicture { get; set; }
    public double OwnerRating { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ViewCount { get; set; }
    public int? BorrowDurationDays { get; set; }
    public DateTime? AvailableFrom { get; set; }
}

public class BookDetailsViewModel
{
    public Book Book { get; set; } = null!;
    public List<BookCardViewModel> SimilarBooks { get; set; } = new();
    public List<Review> Reviews { get; set; } = new();
    public BorrowRequest? ActiveBorrowRequest { get; set; }
    public bool CanBorrow { get; set; }
    public bool IsOwner { get; set; }
    public bool HasReviewed { get; set; }
    public DateTime? AvailableFrom { get; set; }
}

// Dashboard ViewModels
public class DashboardViewModel
{
    public ApplicationUser User { get; set; } = null!;
    public int TotalBooksListed { get; set; }
    public int ActiveBorrows { get; set; }
    public int PendingRequests { get; set; }
    public int TotalTransactions { get; set; }
    public decimal TotalEarnings { get; set; }
    public List<BookCardViewModel> MyBooks { get; set; } = new();
    public List<BorrowRequest> RecentRequests { get; set; } = new();
    public List<Notification> Notifications { get; set; } = new();
    public List<ConversationViewModel> RecentChats { get; set; } = new();
}

// Chat ViewModels
public class ConversationViewModel
{
    public int ConversationId { get; set; }
    public string OtherUserId { get; set; } = string.Empty;
    public string OtherUserName { get; set; } = string.Empty;
    public string? OtherUserPicture { get; set; }
    public bool OtherUserOnline { get; set; }
    public string? LastMessage { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
    public string? BookTitle { get; set; }
}

public class ChatViewModel
{
    public Conversation Conversation { get; set; } = null!;
    public List<Message> Messages { get; set; } = new();
    public ApplicationUser OtherUser { get; set; } = null!;
    public List<ConversationViewModel> AllConversations { get; set; } = new();
}

// Admin ViewModels
public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int TotalBooks { get; set; }
    public int TotalTransactions { get; set; }
    public int PendingTickets { get; set; }
    public int BannedUsers { get; set; }
    public int PendingReports { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<ApplicationUser> RecentUsers { get; set; } = new();
    public List<Book> RecentBooks { get; set; } = new();
    public List<SupportTicket> OpenTickets { get; set; } = new();
    public List<Report> PendingReportsList { get; set; } = new();
}

// Profile ViewModels
public class ProfileViewModel
{
    public ApplicationUser User { get; set; } = null!;
    public List<BookCardViewModel> Books { get; set; } = new();
    public List<Review> Reviews { get; set; } = new();
    public bool IsOwnProfile { get; set; }
}

public class EditProfileViewModel
{
    [Required] public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? CNIC { get; set; }
    public IFormFile? ProfilePicture { get; set; }
    public string? CurrentProfilePicture { get; set; }
}

// Review ViewModel
public class CreateReviewViewModel
{
    [Required, Range(1, 5)] public int Rating { get; set; }
    public string? Comment { get; set; }
    [Required] public string ReviewedUserId { get; set; } = string.Empty;
    public int? BookId { get; set; }
}

// Borrow ViewModel
public class BorrowRequestViewModel
{
    [Required] public int BookId { get; set; }
    [Required, Range(1, 30)] public int BorrowDays { get; set; }
    public string? Message { get; set; }
}

// Home Page ViewModel
public class HomeViewModel
{
    public List<BookCardViewModel> FeaturedBooks { get; set; } = new();
    public List<BookCardViewModel> RecentBooks { get; set; } = new();
    public List<BookCardViewModel> TrendingBooks { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public int TotalBooks { get; set; }
    public int TotalUsers { get; set; }
    public int TotalTransactions { get; set; }
    public int BooksAvailableForFree { get; set; }
}
