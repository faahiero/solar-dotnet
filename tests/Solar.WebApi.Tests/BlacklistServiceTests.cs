using Microsoft.EntityFrameworkCore;
using Solar.Application.Administration;
using Solar.Domain.Entities;
using Solar.Infrastructure.Persistence;
using Xunit;

namespace Solar.WebApi.Tests;

public class BlacklistServiceTests
{
    private SolarDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<SolarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new SolarDbContext(options);
    }

    [Fact]
    public async Task AddToBlacklistAsync_ShouldAddCpfAndMarkAsBlacklisted()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var service = new BlacklistService();
        string cpf = "123.456.789-00";

        // Act
        var entry = await service.AddToBlacklistAsync(cpf, "Fraude em avaliação", 1, db);
        bool isBlacklisted = await service.IsCpfBlacklistedAsync(cpf, db);

        // Assert
        Assert.NotNull(entry);
        Assert.True(entry.Active);
        Assert.Equal("12345678900", entry.Cpf);
        Assert.True(isBlacklisted);
    }

    [Fact]
    public async Task RemoveFromBlacklistAsync_ShouldDeactivateBlacklistEntry()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var service = new BlacklistService();
        string cpf = "999.888.777-66";

        await service.AddToBlacklistAsync(cpf, "Suspensão temporária", 2, db);
        Assert.True(await service.IsCpfBlacklistedAsync(cpf, db));

        // Act
        bool removed = await service.RemoveFromBlacklistAsync(cpf, db);
        bool isBlacklistedAfter = await service.IsCpfBlacklistedAsync(cpf, db);

        // Assert
        Assert.True(removed);
        Assert.False(isBlacklistedAfter);
    }
}
