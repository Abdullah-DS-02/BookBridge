using BookBridge.Models.Entities;
using BookBridge.Models.ViewModels;

namespace BookBridge.Services.Interfaces;

public interface IBookService
{
    Task<BookListViewModel> GetBooksAsync(BookListViewModel filters);
    Task<Book?> GetBookByIdAsync(int id);
    Task<BookDetailsViewModel> GetBookDetailsAsync(int id, string? currentUserId);
    Task<Book> CreateBookAsync(BookCreateViewModel model, string ownerId);
    Task<bool> UpdateBookAsync(int id, BookCreateViewModel model, string userId);
    Task<bool> DeleteBookAsync(int id, string userId);
    Task<List<BookCardViewModel>> GetUserBooksAsync(string userId);
    Task<List<BookCardViewModel>> GetFeaturedBooksAsync(int count = 6);
    Task<List<BookCardViewModel>> GetTrendingBooksAsync(int count = 8);
    Task<List<BookCardViewModel>> GetRecentBooksAsync(int count = 8);
    Task<List<BookCardViewModel>> GetSimilarBooksAsync(int bookId, int count = 4);
    Task IncrementViewCountAsync(int bookId);
}

public interface IUserService
{
    Task<ApplicationUser?> GetUserByIdAsync(string userId);
    Task<ProfileViewModel> GetProfileAsync(string userId, string? currentUserId);
    Task<bool> UpdateProfileAsync(string userId, EditProfileViewModel model);
    Task<bool> UpdateProfilePictureAsync(string userId, IFormFile image);
    Task<DashboardViewModel> GetDashboardAsync(string userId);
    Task<bool> BanUserAsync(string userId, string reason);
    Task<bool> UnbanUserAsync(string userId);
    Task<List<ApplicationUser>> GetAllUsersAsync();
}

public interface IBorrowService
{
    Task<bool> CreateBorrowRequestAsync(BorrowRequestViewModel model, string borrowerId);
    Task<bool> ApproveBorrowRequestAsync(int requestId, string ownerId);
    Task<bool> RejectBorrowRequestAsync(int requestId, string ownerId, string reason);
    Task<bool> ReturnBookAsync(int requestId, string borrowerId);
    Task<List<BorrowRequest>> GetUserBorrowRequestsAsync(string userId);
    Task<List<BorrowRequest>> GetBookBorrowRequestsAsync(int bookId, string ownerId);
    Task CheckOverdueBorrowsAsync();
}

public interface INotificationService
{
    Task CreateNotificationAsync(string userId, string title, string body, string type = "info", string? link = null);
    Task<List<Notification>> GetUserNotificationsAsync(string userId, int count = 20);
    Task<int> GetUnreadCountAsync(string userId);
    Task MarkAsReadAsync(int notificationId, string userId);
    Task MarkAllAsReadAsync(string userId);
}

public interface IChatService
{
    Task<Conversation> GetOrCreateConversationAsync(string user1Id, string user2Id, int? bookId = null);
    Task<ChatViewModel> GetChatViewModelAsync(int conversationId, string userId);
    Task<Message> SendMessageAsync(int conversationId, string senderId, string content, IFormFile? image = null);
    Task<List<ConversationViewModel>> GetUserConversationsAsync(string userId);
    Task MarkMessagesAsReadAsync(int conversationId, string userId);
}

public interface IImageService
{
    Task<string> SaveImageAsync(IFormFile file, string folder);
    Task<List<string>> SaveImagesAsync(List<IFormFile> files, string folder);
    Task DeleteImageAsync(string imagePath);
    string GetDefaultImagePath();
}

public interface IReviewService
{
    Task<Review> CreateReviewAsync(CreateReviewViewModel model, string reviewerId);
    Task<List<Review>> GetUserReviewsAsync(string userId);
    Task<List<Review>> GetBookReviewsAsync(int bookId);
    Task<bool> HasUserReviewedAsync(string reviewerId, string reviewedUserId);
}

public interface ISupportService
{
    Task<SupportTicket> CreateTicketAsync(string userId, string subject, string description);
    Task<List<SupportTicket>> GetUserTicketsAsync(string userId);
    Task<List<SupportTicket>> GetAllTicketsAsync();
    Task<bool> ReplyToTicketAsync(int ticketId, string adminReply);
    Task<bool> CloseTicketAsync(int ticketId);
}

public interface IAdminService
{
    Task<AdminDashboardViewModel> GetDashboardAsync();
    Task<bool> FeatureBookAsync(int bookId);
    Task<bool> RemoveBookAsync(int bookId);
    Task<List<Report>> GetPendingReportsAsync();
    Task<bool> ResolveReportAsync(int reportId, string action);
}
