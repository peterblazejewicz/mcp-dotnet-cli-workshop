// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Mcp.DotNet.CliWorkshop.Core.Services;

/// <summary>
/// Implementation of IDotNetCliService that executes dotnet CLI commands.
/// </summary>
public partial class DotNetCliService(ILogger<DotNetCliService> logger) : IDotNetCliService
{
    private const string DotNetExecutable = "dotnet";

    /// <inheritdoc />
    public async Task<DotNetInfo> GetDotNetInfoAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Executing dotnet --info");
        var output = await this.ExecuteDotNetCommandAsync("--info", cancellationToken).ConfigureAwait(false);

        return this.ParseDotNetInfo(output);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SdkInfo>> GetInstalledSdksAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Executing dotnet --list-sdks");
        var output = await this.ExecuteDotNetCommandAsync("--list-sdks", cancellationToken).ConfigureAwait(false);

        return this.ParseSdkList(output);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RuntimeInfo>> GetInstalledRuntimesAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Executing dotnet --list-runtimes");
        var output = await this.ExecuteDotNetCommandAsync("--list-runtimes", cancellationToken).ConfigureAwait(false);

        return this.ParseRuntimeList(output);
    }

    private async Task<string> ExecuteDotNetCommandAsync(string arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = DotNetExecutable,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                var error = errorBuilder.ToString();
                logger.LogError("dotnet {Arguments} failed with exit code {ExitCode}: {Error}",
                    arguments, process.ExitCode, error);
                throw new InvalidOperationException($"dotnet command failed: {error}");
            }

            var output = outputBuilder.ToString();
            logger.LogDebug("dotnet {Arguments} output: {Output}", arguments, output);
            return output;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            logger.LogError(ex, "Failed to execute dotnet {Arguments}", arguments);
            throw;
        }
    }

    private DotNetInfo ParseDotNetInfo(string output)
    {
        // Parse key information from dotnet --info output
        var sdkVersionMatch = SdkVersionRegex().Match(output);
        var runtimeVersionMatch = RuntimeVersionRegex().Match(output);
        var osVersionMatch = OsVersionRegex().Match(output);
        var architectureMatch = ArchitectureRegex().Match(output);

        return new DotNetInfo(
            SdkVersion: sdkVersionMatch.Success ? sdkVersionMatch.Groups[1].Value : "Unknown",
            RuntimeVersion: runtimeVersionMatch.Success ? runtimeVersionMatch.Groups[1].Value : "Unknown",
            OsVersion: osVersionMatch.Success ? osVersionMatch.Groups[1].Value : "Unknown",
            Architecture: architectureMatch.Success ? architectureMatch.Groups[1].Value : "Unknown",
            RawOutput: output
        );
    }

    private List<SdkInfo> ParseSdkList(string output)
    {
        var sdks = new List<SdkInfo>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines)
        {
            // Format: "9.0.302 [/usr/local/share/dotnet/sdk]"
            var match = SdkLineRegex().Match(line);
            if (match.Success)
            {
                sdks.Add(new SdkInfo(
                    Version: match.Groups[1].Value,
                    Path: match.Groups[2].Value
                ));
            }
        }

        logger.LogInformation("Found {Count} installed SDKs", sdks.Count);
        return sdks;
    }

    private List<RuntimeInfo> ParseRuntimeList(string output)
    {
        var runtimes = new List<RuntimeInfo>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines)
        {
            // Format: "Microsoft.NETCore.App 9.0.3 [/usr/local/share/dotnet/shared/Microsoft.NETCore.App]"
            var match = RuntimeLineRegex().Match(line);
            if (match.Success)
            {
                runtimes.Add(new RuntimeInfo(
                    Name: match.Groups[1].Value,
                    Version: match.Groups[2].Value,
                    Path: match.Groups[3].Value
                ));
            }
        }

        logger.LogInformation("Found {Count} installed runtimes", runtimes.Count);
        return runtimes;
    }

    // Compiled regex patterns for better performance
    [GeneratedRegex(@"Version:\s+(.+)", RegexOptions.Multiline)]
    private static partial Regex SdkVersionRegex();

    [GeneratedRegex(@"Microsoft\.NETCore\.App\s+([\d\.]+)", RegexOptions.Multiline)]
    private static partial Regex RuntimeVersionRegex();

    [GeneratedRegex(@"OS\s+(?:Version|Name):\s+(.+)", RegexOptions.Multiline)]
    private static partial Regex OsVersionRegex();

    [GeneratedRegex(@"Architecture:\s+(.+)", RegexOptions.Multiline)]
    private static partial Regex ArchitectureRegex();

    [GeneratedRegex(@"^([\d\.]+(?:-[\w\.]+)?)\s+\[([^\]]+)\]")]
    private static partial Regex SdkLineRegex();

    [GeneratedRegex(@"^([\w\.]+)\s+([\d\.]+(?:-[\w\.]+)?)\s+\[([^\]]+)\]")]
    private static partial Regex RuntimeLineRegex();
}
