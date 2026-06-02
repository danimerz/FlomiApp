using FlomiApp.Data;
using FlomiApp.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlomiApp.Services;

public class ChatService : IChatService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public event Action<ChatMessage>? OnMessageSent;

    public ChatService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    private ApplicationDbContext CreateDb()
    {
        var scope = _scopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public async Task<List<ChatMessage>> GetConversationAsync(string userId1, string userId2)
    {
        using var db = CreateDb();
        return await db.ChatMessages
            .Where(m => (m.FromUserId == userId1 && m.ToUserId == userId2)
                     || (m.FromUserId == userId2 && m.ToUserId == userId1))
            .OrderBy(m => m.SentAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<ApplicationUser>> GetUsersWithMessagesAsync(string adminId)
    {
        using var db = CreateDb();
        var userIds = await db.ChatMessages
            .Where(m => m.FromUserId == adminId || m.ToUserId == adminId)
            .Select(m => m.FromUserId == adminId ? m.ToUserId : m.FromUserId)
            .Distinct()
            .ToListAsync();

        return await db.Users
            .Where(u => userIds.Contains(u.Id))
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(string toUserId, string? fromUserId = null)
    {
        using var db = CreateDb();
        var query = db.ChatMessages
            .Where(m => m.ToUserId == toUserId && !m.IsRead);
        if (fromUserId != null)
            query = query.Where(m => m.FromUserId == fromUserId);
        return await query.CountAsync();
    }

    public async Task<ChatMessage> SendMessageAsync(string fromId, string toId, string body, bool isFromAdmin)
    {
        using var db = CreateDb();
        var msg = new ChatMessage
        {
            FromUserId  = fromId,
            ToUserId    = toId,
            Body        = body.Trim(),
            SentAt      = DateTime.Now,
            IsRead      = false,
            IsFromAdmin = isFromAdmin
        };
        db.ChatMessages.Add(msg);
        await db.SaveChangesAsync();

        // Für UI-Anzeige: Absender-Namen nachladen
        msg.From = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == fromId) ?? new ApplicationUser();
        msg.To   = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == toId)   ?? new ApplicationUser();

        // Event feuern → alle offenen Blazor-Circuits werden benachrichtigt
        OnMessageSent?.Invoke(msg);
        return msg;
    }

    public async Task DeleteConversationAsync(string userId1, string userId2)
    {
        using var db = CreateDb();
        var msgs = await db.ChatMessages
            .Where(m => (m.FromUserId == userId1 && m.ToUserId == userId2)
                     || (m.FromUserId == userId2 && m.ToUserId == userId1))
            .ToListAsync();
        db.ChatMessages.RemoveRange(msgs);
        await db.SaveChangesAsync();
    }

    public async Task MarkReadAsync(string readerUserId, string otherUserId)
    {
        using var db = CreateDb();
        var unread = await db.ChatMessages
            .Where(m => m.ToUserId == readerUserId && m.FromUserId == otherUserId && !m.IsRead)
            .ToListAsync();
        unread.ForEach(m => m.IsRead = true);
        await db.SaveChangesAsync();
    }
}
