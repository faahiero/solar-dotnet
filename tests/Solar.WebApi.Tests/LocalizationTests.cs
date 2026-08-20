using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Solar.WebApi.Tests;

public class LocalizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LocalizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        });
    }

    [Fact]
    public async Task Get_Locales_Should_Return_Supported_Cultures()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/locales");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LocalesResponse>();
        result.Should().NotBeNull();
        result!.SupportedCultures.Should().HaveCount(2);
        result.SupportedCultures.Should().Contain(c => c.Code == "pt-BR" && c.IsDefault);
        result.SupportedCultures.Should().Contain(c => c.Code == "en-US" && !c.IsDefault);
    }

    [Fact]
    public async Task Get_Locales_With_Culture_Query_Param_Should_Resolve_CurrentCulture()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/locales?culture=en-US");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LocalesResponse>();
        result.Should().NotBeNull();
        result!.CurrentCulture.Should().Be("en-US");
    }

    [Fact]
    public async Task Get_Locales_With_AcceptLanguage_Header_Should_Resolve_Culture()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.Clear();
        client.DefaultRequestHeaders.AcceptLanguage.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("en-US"));

        // Act
        var response = await client.GetAsync("/api/v1/locales");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LocalesResponse>();
        result.Should().NotBeNull();
        result!.CurrentCulture.Should().Be("en-US");
    }

    private record LocalesResponse(string CurrentCulture, CultureItem[] SupportedCultures);
    private record CultureItem(string Code, string Name, string Flag, bool IsDefault);
}
