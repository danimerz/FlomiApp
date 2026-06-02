using FlomiApp.Data.Models;

namespace FlomiApp.Services;

public interface IChatService
{
    /// Feuert wenn irgendeine Nachricht gesendet wird — Blazor-Circuits abonnieren und prüfen ob relevant.
    event Action<ChatMessage> OnMessageSent;

    Task<List<ChatMessage>>        GetConversationAsync(string userId1, string userId2);
    Task<List<ApplicationUser>>    GetUsersWithMessagesAsync(string adminId);
    Task<int>                      GetUnreadCountAsync(string toUserId, string? fromUserId = null);
    Task<ChatMessage>              SendMessageAsync(string fromId, string toId, string body, bool isFromAdmin);
    Task                           MarkReadAsync(string readerUserId, string otherUserId);
}
