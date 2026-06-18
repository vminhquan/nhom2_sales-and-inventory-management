using Microsoft.EntityFrameworkCore;
using nhom2.Domain.Entities;
using nhom2.Domain.Interfaces;
using nhom2.Infrastructure.Data;

namespace nhom2.Infrastructure.Repositories;

public class ChatSessionRepo : IChatSession
{
    private readonly ApplicationDbContext _context;

    public ChatSessionRepo(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ChatSession> GetOrCreateActiveAsync(int customerUserId, string? customerEmail)
    {
        var session = await _context.ChatSessions
            .FirstOrDefaultAsync(item => item.CustomerUserId == customerUserId && item.IsActive);

        if (session is not null)
        {
            session.LastActivityAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(customerEmail))
                session.CustomerEmail = customerEmail.Trim();
            await _context.SaveChangesAsync();
            return session;
        }

        session = new ChatSession
        {
            CustomerUserId = customerUserId,
            CustomerEmail = string.IsNullOrWhiteSpace(customerEmail) ? null : customerEmail.Trim(),
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow
        };
        _context.ChatSessions.Add(session);
        await _context.SaveChangesAsync();
        return session;
    }

    public Task<ChatSession?> GetActiveAsync(int customerUserId)
    {
        return _context.ChatSessions
            .FirstOrDefaultAsync(item => item.CustomerUserId == customerUserId && item.IsActive);
    }

    public Task<List<ChatMessage>> GetMessagesAsync(int sessionId, int limit = 30)
    {
        return _context.ChatMessages
            .Where(message => message.ChatSessionId == sessionId)
            .OrderByDescending(message => message.CreatedAt)
            .Take(limit)
            .OrderBy(message => message.CreatedAt)
            .ToListAsync();
    }

    public async Task AddMessageAsync(ChatMessage message)
    {
        _context.ChatMessages.Add(message);
        await _context.ChatSessions
            .Where(session => session.Id == message.ChatSessionId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(session => session.LastActivityAt, DateTime.UtcNow));
        await _context.SaveChangesAsync();
    }

    public async Task EndActiveAsync(int customerUserId)
    {
        var sessions = await _context.ChatSessions
            .Where(session => session.CustomerUserId == customerUserId && session.IsActive)
            .ToListAsync();

        foreach (var session in sessions)
        {
            session.IsActive = false;
            session.EndedAt = DateTime.UtcNow;
            session.LastActivityAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }
}
