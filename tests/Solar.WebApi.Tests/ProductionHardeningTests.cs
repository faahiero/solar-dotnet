using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Solar.Domain.Entities;
using Solar.Infrastructure.Background;
using Solar.Infrastructure.Persistence;
using Xunit;

namespace Solar.WebApi.Tests;

public class ProductionHardeningTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProductionHardeningTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        });
    }

    [Fact]
    public async Task AdminMetrics_Endpoint_Should_Return_200_OK_With_Aggregated_Telemetry()
    {
        // Arrange
        var client = _factory.CreateClient().AsAdmin();

        // Act
        var response = await client.GetAsync("/api/v1/admin/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("users").GetProperty("total").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        json.GetProperty("allocations").GetProperty("total").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        json.GetProperty("academic").GetProperty("groups").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        json.GetProperty("system").GetProperty("framework").GetString().Should().Contain(".NET 10");
    }

    [Fact]
    public async Task AcademicMaintenanceWorker_Should_AutoSubmit_Expired_ExamAttempts()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SolarDbContext>();

        var expiredAttempt = new ExamUserAttempt
        {
            AcademicAllocationUserId = 1,
            Complete = false,
            Start = DateTime.UtcNow.AddHours(-30),
            CreatedAt = DateTime.UtcNow.AddHours(-30),
            UpdatedAt = DateTime.UtcNow.AddHours(-30)
        };
        db.ExamUserAttempts.Add(expiredAttempt);
        await db.SaveChangesAsync();

        var worker = new AcademicMaintenanceWorker(
            _factory.Services,
            NullLogger<AcademicMaintenanceWorker>.Instance
        );

        // Act
        await worker.PerformPeriodicMaintenanceAsync(CancellationToken.None);

        // Assert
        var updatedAttempt = await db.ExamUserAttempts.AsNoTracking().FirstOrDefaultAsync(e => e.Id == expiredAttempt.Id);
        updatedAttempt.Should().NotBeNull();
        updatedAttempt!.Complete.Should().BeTrue();
        updatedAttempt.End.Should().NotBeNull();
    }
}
