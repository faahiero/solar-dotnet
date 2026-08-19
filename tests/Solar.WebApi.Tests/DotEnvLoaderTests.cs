using System;
using System.IO;
using FluentAssertions;
using Solar.Infrastructure.Configuration;
using Xunit;

namespace Solar.WebApi.Tests;

public class DotEnvLoaderTests : IDisposable
{
    private readonly string _tempDir;

    public DotEnvLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SolarEnvTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch { }
    }

    [Fact]
    public void Load_Should_Parse_And_Set_Environment_Variables()
    {
        // Arrange
        var envFilePath = Path.Combine(_tempDir, ".env");
        var testKey = "TEST_SOLAR_KEY_" + Guid.NewGuid().ToString("N")[..8];
        var testVal = "test_value_solar_123";

        File.WriteAllLines(envFilePath, new[]
        {
            "# Comentario",
            $"{testKey}={testVal}",
            "ANOTHER_KEY=\"quoted_value\""
        });

        // Act
        DotEnvLoader.Load(_tempDir);

        // Assert
        Environment.GetEnvironmentVariable(testKey).Should().Be(testVal);
        Environment.GetEnvironmentVariable("ANOTHER_KEY").Should().Be("quoted_value");
    }

    [Fact]
    public void Load_Should_Not_Throw_When_Env_File_Missing()
    {
        // Act
        var act = () => DotEnvLoader.Load(_tempDir);

        // Assert
        act.Should().NotThrow();
    }
}
