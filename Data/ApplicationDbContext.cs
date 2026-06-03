using BookBridge.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BookBridge.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Book> Books { get; set; }
    public DbSet<BookImage> BookImages { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<BorrowRequest> BorrowRequests { get; set; }
    public DbSet<ExchangeRequest> ExchangeRequests { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<SupportTicket> SupportTickets { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<NewsletterSubscription> NewsletterSubscriptions { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Book relationships
        builder.Entity<Book>()
            .HasOne(b => b.Owner)
            .WithMany(u => u.Books)
            .HasForeignKey(b => b.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Book>()
            .HasOne(b => b.Category)
            .WithMany(c => c.Books)
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // BorrowRequest relationships
        builder.Entity<BorrowRequest>()
            .HasOne(br => br.Borrower)
            .WithMany(u => u.BorrowRequestsMade)
            .HasForeignKey(br => br.BorrowerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<BorrowRequest>()
            .HasOne(br => br.Owner)
            .WithMany(u => u.BorrowRequestsReceived)
            .HasForeignKey(br => br.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Review relationships
        builder.Entity<Review>()
            .HasOne(r => r.Reviewer)
            .WithMany(u => u.ReviewsGiven)
            .HasForeignKey(r => r.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Review>()
            .HasOne(r => r.ReviewedUser)
            .WithMany(u => u.ReviewsReceived)
            .HasForeignKey(r => r.ReviewedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Transaction relationships
        builder.Entity<Transaction>()
            .HasOne(t => t.Buyer)
            .WithMany(u => u.Transactions)
            .HasForeignKey(t => t.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Transaction>()
            .HasOne(t => t.Seller)
            .WithMany()
            .HasForeignKey(t => t.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Transaction>()
            .Property(t => t.Amount)
            .HasPrecision(18, 2);

        builder.Entity<ApplicationUser>()
            .Property(u => u.WalletBalance)
            .HasPrecision(18, 2);

        builder.Entity<Book>()
            .Property(b => b.Price)
            .HasPrecision(18, 2);

        builder.Entity<Book>()
            .Property(b => b.DepositAmount)
            .HasPrecision(18, 2);

        // Conversation relationships
        builder.Entity<Conversation>()
            .HasOne(c => c.User1)
            .WithMany()
            .HasForeignKey(c => c.User1Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Conversation>()
            .HasOne(c => c.User2)
            .WithMany()
            .HasForeignKey(c => c.User2Id)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.Entity<Book>()
            .HasIndex(b => b.Status);

        builder.Entity<Book>()
            .HasIndex(b => b.TransactionType);

        builder.Entity<Book>()
            .HasIndex(b => b.CategoryId);

        builder.Entity<Notification>()
            .HasIndex(n => n.UserId);

        // ExchangeRequest relationships
        builder.Entity<ExchangeRequest>()
            .HasOne(er => er.Requester)
            .WithMany()
            .HasForeignKey(er => er.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ExchangeRequest>()
            .HasOne(er => er.RequestedBook)
            .WithMany(b => b.ExchangeRequests)
            .HasForeignKey(er => er.RequestedBookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ExchangeRequest>()
            .HasOne(er => er.OfferedBook)
            .WithMany()
            .HasForeignKey(er => er.OfferedBookId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed Categories
        builder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Fiction", Icon = "bi-book", Description = "Novels, stories, and literary fiction" },
            new Category { Id = 2, Name = "Non-Fiction", Icon = "bi-journal-text", Description = "Biographies, self-help, and real-world stories" },
            new Category { Id = 3, Name = "Science & Technology", Icon = "bi-cpu", Description = "Computing, engineering, and sciences" },
            new Category { Id = 4, Name = "Business", Icon = "bi-briefcase", Description = "Finance, marketing, and entrepreneurship" },
            new Category { Id = 5, Name = "Academic", Icon = "bi-mortarboard", Description = "Textbooks and study materials" },
            new Category { Id = 6, Name = "Children", Icon = "bi-star", Description = "Books for kids and young readers" },
            new Category { Id = 7, Name = "History", Icon = "bi-hourglass", Description = "Historical events and civilizations" },
            new Category { Id = 8, Name = "Religion & Philosophy", Icon = "bi-heart", Description = "Spiritual and philosophical works" },
            new Category { Id = 9, Name = "Arts & Design", Icon = "bi-palette", Description = "Art, architecture, and creative design" },
            new Category { Id = 10, Name = "Health & Wellness", Icon = "bi-heart-pulse", Description = "Medicine, fitness, and mental health" }
        );
    }
}
