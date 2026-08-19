using Microsoft.EntityFrameworkCore;
using Solar.Domain.Entities;

namespace Solar.Application.Administration;

public interface IBlacklistDbContext
{
    DbSet<UserBlacklist> UserBlacklists { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
