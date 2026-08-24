using System.IO.Compression;
using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Solar.WebApi.Tests;

public class BatchFileDownloadTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BatchFileDownloadTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        });
    }

    [Fact]
    public async Task DownloadAssignmentSubmissionsZip_Should_Return_Valid_ZipArchive()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/curriculum-units/1/assignments/1/download-all-zip");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/zip");
        response.Content.Headers.ContentDisposition?.FileName.Should().Contain(".zip");

        var zipBytes = await response.Content.ReadAsByteArrayAsync();
        zipBytes.Length.Should().BeGreaterThan(0);

        using var memoryStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);
        archive.Entries.Should().NotBeEmpty();

        var firstEntry = archive.Entries[0];
        using var stream = firstEntry.Open();
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        content.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task DownloadCurriculumUnitMaterialsZip_Should_Return_Valid_ZipArchive_With_Syllabus()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/curriculum-units/1/materials/download-zip");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/zip");
        response.Content.Headers.ContentDisposition?.FileName.Should().Contain(".zip");

        var zipBytes = await response.Content.ReadAsByteArrayAsync();
        zipBytes.Length.Should().BeGreaterThan(0);

        using var memoryStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);
        archive.Entries.Count.Should().BeGreaterThanOrEqualTo(2);

        var hasSyllabus = archive.Entries.Any(e => e.Name.Contains("Ementa_e_Plano_de_Ensino"));
        var hasGuide = archive.Entries.Any(e => e.Name.Contains("Guia_do_Estudante"));

        hasSyllabus.Should().BeTrue();
        hasGuide.Should().BeTrue();
    }
}
