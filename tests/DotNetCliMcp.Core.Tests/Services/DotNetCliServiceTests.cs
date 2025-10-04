using DotNetCliMcp.Core.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace DotNetCliMcp.Core.Tests.Services;

public class DotNetCliServiceTests
{
    private readonly ILogger<DotNetCliService> _mockLogger;
    private readonly DotNetCliService _service;

    public DotNetCliServiceTests()
    {
        _mockLogger = Substitute.For<ILogger<DotNetCliService>>();
        _service = new DotNetCliService(_mockLogger);
    }

    [Fact]
    public async Task GetDotNetInfoAsync_ShouldReturnDotNetInfo()
    {
        // Act
        var result = await _service.GetDotNetInfoAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.SdkVersion);
        Assert.NotEmpty(result.RawOutput);
    }

    [Fact]
    public async Task GetInstalledSdksAsync_ShouldReturnListOfSdks()
    {
        // Act
        var result = await _service.GetInstalledSdksAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, sdk =>
        {
            Assert.NotEmpty(sdk.Version);
            Assert.NotEmpty(sdk.Path);
        });
    }

    [Fact]
    public async Task GetInstalledRuntimesAsync_ShouldReturnListOfRuntimes()
    {
        // Act
        var result = await _service.GetInstalledRuntimesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, runtime =>
        {
            Assert.NotEmpty(runtime.Name);
            Assert.NotEmpty(runtime.Version);
            Assert.NotEmpty(runtime.Path);
        });
    }

    [Fact]
    public async Task GetDotNetInfoAsync_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _service.GetDotNetInfoAsync(cts.Token)
        );
    }

    [Fact]
    public async Task GetInstalledSdksAsync_ShouldParseSdkVersionsCorrectly()
    {
        // Act
        var sdks = await _service.GetInstalledSdksAsync();

        // Assert
        Assert.All(sdks, sdk =>
        {
            // Version should match pattern: major.minor.patch or major.minor.patch-preview
            Assert.Matches(@"^\d+\.\d+\.\d+", sdk.Version);
        });
    }
}
