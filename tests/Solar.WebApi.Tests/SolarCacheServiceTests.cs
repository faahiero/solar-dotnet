using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Solar.Infrastructure.Caching;
using Xunit;

namespace Solar.WebApi.Tests;

public class SolarCacheServiceTests
{
    [Fact]
    public async Task GetOrCreateAsync_WhenCacheMiss_ShouldExecuteFactoryAndStoreResult()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cacheService = new SolarMemoryCacheService(memoryCache, NullLogger<SolarMemoryCacheService>.Instance);
        var executionCount = 0;

        // Act
        var result = await cacheService.GetOrCreateAsync("test_key", async () =>
        {
            executionCount++;
            await Task.Yield();
            return "Solar LMS 2.0";
        });

        // Assert
        result.Should().Be("Solar LMS 2.0");
        executionCount.Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenCacheHit_ShouldReturnCachedValueWithoutReExecutingFactory()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cacheService = new SolarMemoryCacheService(memoryCache, NullLogger<SolarMemoryCacheService>.Instance);
        var executionCount = 0;

        // Act - First call (Miss)
        var firstResult = await cacheService.GetOrCreateAsync("curriculum_unit_1", async () =>
        {
            executionCount++;
            await Task.Yield();
            return new { Id = 1, Name = "Linguística" };
        });

        // Act - Second call (Hit)
        var secondResult = await cacheService.GetOrCreateAsync("curriculum_unit_1", async () =>
        {
            executionCount++;
            await Task.Yield();
            return new { Id = 1, Name = "Outro Nome" };
        });

        // Assert
        firstResult.Should().BeEquivalentTo(secondResult);
        executionCount.Should().Be(1, "o segundo acesso deve vir direto da memória RAM sem reexecutar o factory");
    }

    [Fact]
    public async Task Remove_ShouldInvalidateSpecificKey()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cacheService = new SolarMemoryCacheService(memoryCache, NullLogger<SolarMemoryCacheService>.Instance);
        var executionCount = 0;

        await cacheService.GetOrCreateAsync("cache_item_to_remove", async () =>
        {
            executionCount++;
            await Task.Yield();
            return "Initial Value";
        });

        // Act
        cacheService.Remove("cache_item_to_remove");

        var afterRemoveResult = await cacheService.GetOrCreateAsync("cache_item_to_remove", async () =>
        {
            executionCount++;
            await Task.Yield();
            return "New Value";
        });

        // Assert
        afterRemoveResult.Should().Be("New Value");
        executionCount.Should().Be(2, "após a invalidação, a chave deve ser recalculada");
    }

    [Fact]
    public async Task RemoveByPrefix_ShouldInvalidateAllMatchingPrefixKeys()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cacheService = new SolarMemoryCacheService(memoryCache, NullLogger<SolarMemoryCacheService>.Instance);

        await cacheService.GetOrCreateAsync("contacts_1_user_5", () => Task.FromResult("Contact List 1"));
        await cacheService.GetOrCreateAsync("contacts_2_user_5", () => Task.FromResult("Contact List 2"));
        await cacheService.GetOrCreateAsync("unrelated_key", () => Task.FromResult("Unrelated"));

        // Act
        cacheService.RemoveByPrefix("contacts_");

        var c1Recomputed = false;
        var c1 = await cacheService.GetOrCreateAsync("contacts_1_user_5", () =>
        {
            c1Recomputed = true;
            return Task.FromResult("Fresh Contacts");
        });

        var unrelatedRecomputed = false;
        var unrelated = await cacheService.GetOrCreateAsync("unrelated_key", () =>
        {
            unrelatedRecomputed = true;
            return Task.FromResult("Should not be called");
        });

        // Assert
        c1Recomputed.Should().BeTrue("a chave com prefixo contacts_ deve ter sido invalidada");
        unrelatedRecomputed.Should().BeFalse("a chave sem o prefixo deve permanecer em cache");
    }
}
