using BookBridge.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace BookBridge.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private static readonly ConcurrentDictionary<string, string> OnlineUsers = new();
    private readonly IChatService _chatService;
    private readonly INotificationService _notificationService;

    public ChatHub(IChatService chatService, INotificationService notificationService)
    {
        _chatService = chatService;
        _notificationService = notificationService;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier!;
        OnlineUsers[userId] = Context.ConnectionId;
        await Clients.All.SendAsync("UserOnline", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier!;
        OnlineUsers.TryRemove(userId, out _);
        await Clients.All.SendAsync("UserOffline", userId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(int conversationId, string content)
    {
        var senderId = Context.UserIdentifier!;
        var message = await _chatService.SendMessageAsync(conversationId, senderId, content);

        await Clients.Group($"conv_{conversationId}").SendAsync("ReceiveMessage", new
        {
            id = message.Id,
            content = message.Content,
            senderId = message.SenderId,
            sentAt = message.SentAt.ToString("HH:mm"),
            imagePath = message.ImagePath
        });
    }

    public async Task JoinConversation(int conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"conv_{conversationId}");
        await _chatService.MarkMessagesAsReadAsync(conversationId, Context.UserIdentifier!);
    }

    public async Task LeaveConversation(int conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conv_{conversationId}");
    }

    public async Task Typing(int conversationId, bool isTyping)
    {
        await Clients.OthersInGroup($"conv_{conversationId}")
            .SendAsync("UserTyping", Context.UserIdentifier, isTyping);
    }

    public static bool IsUserOnline(string userId) => OnlineUsers.ContainsKey(userId);
}

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier!;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        await base.OnConnectedAsync();
    }
}
