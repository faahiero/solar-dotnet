using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Solar.Domain.Entities;

namespace Solar.Application.Administration;

public class BlacklistService
{
    public async Task<bool> IsCpfBlacklistedAsync(string cpf, IBlacklistDbContext db)
    {
        if (string.IsNullOrWhiteSpace(cpf)) return false;
        var cleanCpf = Regex.Replace(cpf, @"[^\d]", "");
        return await db.UserBlacklists.AnyAsync(b => b.Cpf == cleanCpf || b.Cpf == cpf);
    }

    public async Task<UserBlacklist> AddToBlacklistAsync(string cpf, string reason, long? userId, IBlacklistDbContext db)
    {
        var cleanCpf = Regex.Replace(cpf, @"[^\d]", "");
        var existing = await db.UserBlacklists.FirstOrDefaultAsync(b => b.Cpf == cleanCpf || b.Cpf == cpf);

        if (existing != null)
        {
            existing.Reason = reason;
            existing.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return existing;
        }

        var entry = new UserBlacklist
        {
            Cpf = cleanCpf,
            UserId = userId,
            Reason = reason,
            Active = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.UserBlacklists.Add(entry);
        await db.SaveChangesAsync();
        return entry;
    }

    public async Task<bool> RemoveFromBlacklistAsync(string cpf, IBlacklistDbContext db)
    {
        var cleanCpf = Regex.Replace(cpf, @"[^\d]", "");
        var entries = await db.UserBlacklists.Where(b => b.Cpf == cleanCpf || b.Cpf == cpf).ToListAsync();

        if (!entries.Any()) return false;

        db.UserBlacklists.RemoveRange(entries);
        await db.SaveChangesAsync();
        return true;
    }
}
