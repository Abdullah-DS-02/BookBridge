// Models/Entities/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;

namespace BookBridge.Models.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePicture { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? CNIC { get; set; }
    public bool IsVerified { get; set; } = false;
    public bool IsBanned { get; set; } = false;
    public string? BanReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastActive { get; set; }
    public decimal WalletBalance { get; set; } = 0;
    public double AverageRating { get; set; } = 0;
    public int TotalRatings { get; set; } = 0;

    // Navigation
    public ICollection<Book> Books { get; set; } = new List<Book>();
    public ICollection<BorrowRequest> BorrowRequestsMade { get; set; } = new List<BorrowRequest>();
    public ICollection<BorrowRequest> BorrowRequestsReceived { get; set; } = new List<BorrowRequest>();
    public ICollection<Review> ReviewsGiven { get; set; } = new List<Review>();
    public ICollection<Review> ReviewsReceived { get; set; } = new List<Review>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<SupportTicket> SupportTickets { get; set; } = new List<SupportTicket>();
}
