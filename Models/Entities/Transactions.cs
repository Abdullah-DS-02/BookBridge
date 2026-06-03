namespace BookBridge.Models.Entities;

public enum RequestStatus { Pending, Approved, Rejected, Cancelled, Completed }
public enum TransactionStatus { Pending, Completed, Refunded, Failed }

public class BorrowRequest
{
    public int Id { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? BorrowedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public int BorrowDays { get; set; }
    public string? Message { get; set; }
    public string? RejectionReason { get; set; }
    public bool IsLate { get; set; } = false;

    public string BorrowerId { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public int BookId { get; set; }

    public ApplicationUser Borrower { get; set; } = null!;
    public ApplicationUser Owner { get; set; } = null!;
    public Book Book { get; set; } = null!;
}

public class ExchangeRequest
{
    public int Id { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string RequesterId { get; set; } = string.Empty;
    public int RequestedBookId { get; set; }
    public int? OfferedBookId { get; set; }

    public ApplicationUser Requester { get; set; } = null!;
    public Book RequestedBook { get; set; } = null!;
    public Book? OfferedBook { get; set; }
}

public class Transaction
{
    public int Id { get; set; }
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public decimal Amount { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public string? PaymentMethod { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }

    public string BuyerId { get; set; } = string.Empty;
    public string SellerId { get; set; } = string.Empty;
    public int BookId { get; set; }

    public ApplicationUser Buyer { get; set; } = null!;
    public ApplicationUser Seller { get; set; } = null!;
    public Book Book { get; set; } = null!;
}

public class Review
{
    public int Id { get; set; }
    public int Rating { get; set; } // 1-5
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string ReviewerId { get; set; } = string.Empty;
    public string ReviewedUserId { get; set; } = string.Empty;
    public int? BookId { get; set; }

    public ApplicationUser Reviewer { get; set; } = null!;
    public ApplicationUser ReviewedUser { get; set; } = null!;
    public Book? Book { get; set; }
}

public class Conversation
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastMessageAt { get; set; }

    public string User1Id { get; set; } = string.Empty;
    public string User2Id { get; set; } = string.Empty;
    public int? BookId { get; set; }

    public ApplicationUser User1 { get; set; } = null!;
    public ApplicationUser User2 { get; set; } = null!;
    public Book? Book { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}

public class Message
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public string SenderId { get; set; } = string.Empty;
    public int ConversationId { get; set; }

    public ApplicationUser Sender { get; set; } = null!;
    public Conversation Conversation { get; set; } = null!;
}

public class Notification
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Link { get; set; }
    public string Type { get; set; } = "info";
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
}

public class SupportTicket
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public string Priority { get; set; } = "Normal";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public string? AdminReply { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
}

public class Report
{
    public int Id { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string ReporterId { get; set; } = string.Empty;
    public string? ReportedUserId { get; set; }
    public int? ReportedBookId { get; set; }

    public ApplicationUser Reporter { get; set; } = null!;
}
