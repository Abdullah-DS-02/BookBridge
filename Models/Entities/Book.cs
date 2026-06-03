namespace BookBridge.Models.Entities;

public enum BookCondition { New, LikeNew, Good, Fair, Poor }
public enum TransactionType { Sell, Donate, Borrow, Exchange }
public enum BookStatus { Available, Borrowed, Sold, Donated, Exchanged, Pending }

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? ISBN { get; set; }
    public string? Description { get; set; }
    public string? Publisher { get; set; }
    public int? PublishedYear { get; set; }
    public string Language { get; set; } = "English";
    public int? Pages { get; set; }
    public BookCondition Condition { get; set; }
    public TransactionType TransactionType { get; set; }
    public BookStatus Status { get; set; } = BookStatus.Available;
    public decimal? Price { get; set; }
    public decimal? DepositAmount { get; set; }
    public int? BorrowDurationDays { get; set; }
    public string? Location { get; set; }
    public string? City { get; set; }
    public bool IsFeatured { get; set; } = false;
    public int ViewCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // FK
    public string OwnerId { get; set; } = string.Empty;
    public int CategoryId { get; set; }

    // Navigation
    public ApplicationUser Owner { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<BookImage> Images { get; set; } = new List<BookImage>();
    public ICollection<BorrowRequest> BorrowRequests { get; set; } = new List<BorrowRequest>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<ExchangeRequest> ExchangeRequests { get; set; } = new List<ExchangeRequest>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

public class BookImage
{
    public int Id { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public bool IsPrimary { get; set; } = false;
    public int BookId { get; set; }
    public Book Book { get; set; } = null!;
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public int BookCount { get; set; } = 0;
    public ICollection<Book> Books { get; set; } = new List<Book>();
}
