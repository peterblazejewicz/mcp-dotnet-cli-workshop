// Copyright (c) Microsoft. All rights reserved.

using DotNetCliMcp.Core.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DotNetCliMcp.Core.Tests.Services;

public class DotNetCliServiceTests
{
    private readonly ILogger<DotNetCliService> _mockLogger = Substitute.For<ILogger<DotNetCliService>>();
    private readonly DotNetCliService _service;

    public DotNetCliServiceTests() => this._service = new DotNetCliService(this._mockLogger);

    [Fact]
    public async Task GetDotNetInfo_ShouldReturnDotNetInfoAsync()
    {
        // Act
        var result = await this._service.GetDotNetInfoAsync().ConfigureAwait(true);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.SdkVersion);
        Assert.NotEmpty(result.RawOutput);
    }

    [Fact]
    public async Task GetInstalledSdks_ShouldReturnListOfSdksAsync()
    {
        // Act
        var result = await this._service.GetInstalledSdksAsync().ConfigureAwait(true);

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
    public async Task GetInstalledRuntimes_ShouldReturnListOfRuntimesAsync()
    {
        // Act
        var result = await this._service.GetInstalledRuntimesAsync().ConfigureAwait(true);

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
    public async Task GetDotNetInfo_WithCancellation_ShouldRespectCancellationTokenAsync()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => this._service.GetDotNetInfoAsync(cts.Token)
        ).ConfigureAwait(true);
    }

    [Fact]
    public async Task GetInstalledSdks_ShouldParseSdkVersionsCorrectlyAsync()
    {
        // Act
        var sdks = await this._service.GetInstalledSdksAsync().ConfigureAwait(true);

        // Assert
        Assert.All(sdks, sdk =>
        {
            // Version should match pattern: major.minor.patch or major.minor.patch-preview
            Assert.Matches(@"^\d+\.\d+\.\d+", sdk.Version);
        });
    }
}
