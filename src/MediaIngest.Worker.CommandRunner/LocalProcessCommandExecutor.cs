using System.Diagnostics;
using System.Text;
using MediaIngest.Contracts.Commands;

namespace MediaIngest.Worker.CommandRunner;

public sealed class LocalProcessCommandExecutor : ICommandExecutor
{
    private readonly TimeSpan timeout;
    private readonly int maxCapturedOutputCharacters;

    public LocalProcessCommandExecutor(TimeSpan timeout, int maxCapturedOutputCharacters = 8192)
    {
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout must be greater than zero.");
        }

        if (maxCapturedOutputCharacters < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCapturedOutputCharacters),
                maxCapturedOutputCharacters,
                "Captured output character limit must be zero or greater.");
        }

        this.timeout = timeout;
        this.maxCapturedOutputCharacters = maxCapturedOutputCharacters;
    }

    public async Task<CommandExecutionResult> ExecuteAsync(
        MediaCommandEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (string.IsNullOrWhiteSpace(envelope.CommandLine))
        {
            return CommandExecutionResult.Failed("Command line is required.");
        }

        if (string.IsNullOrWhiteSpace(envelope.WorkingDirectory))
        {
            return CommandExecutionResult.Failed("Working directory is required.");
        }

        if (!Directory.Exists(envelope.WorkingDirectory))
        {
            return CommandExecutionResult.Failed($"Working directory does not exist: {envelope.WorkingDirectory}");
        }

        using var process = new Process
        {
            StartInfo = CreateStartInfo(envelope.CommandLine, envelope.WorkingDirectory),
            EnableRaisingEvents = true
        };

        process.Start();

        var stdoutTask = ReadBoundedAsync(process.StandardOutput, cancellationToken);
        var stderrTask = ReadBoundedAsync(process.StandardError, cancellationToken);

        using var timeoutTokenSource = new CancellationTokenSource(timeout);
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutTokenSource.Token);

        try
        {
            await process.WaitForExitAsync(linkedTokenSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            KillProcessTree(process);
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);

            return CommandExecutionResult.Failed($"timed-out after {timeout}");
        }

        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);
        var message = FormatMessage(process.ExitCode, stdout, stderr);

        return process.ExitCode == 0
            ? CommandExecutionResult.Succeeded(message)
            : CommandExecutionResult.Failed(message);
    }

    private static ProcessStartInfo CreateStartInfo(string commandLine, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (OperatingSystem.IsWindows())
        {
            startInfo.FileName = "cmd.exe";
            startInfo.ArgumentList.Add("/d");
            startInfo.ArgumentList.Add("/s");
            startInfo.ArgumentList.Add("/c");
            startInfo.ArgumentList.Add(commandLine);
        }
        else
        {
            startInfo.FileName = "/bin/sh";
            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add(commandLine);
        }

        return startInfo;
    }

    private async Task<CapturedOutput> ReadBoundedAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder(capacity: Math.Min(maxCapturedOutputCharacters, 1024));
        var buffer = new char[1024];
        var captured = 0;
        var truncated = false;

        while (true)
        {
            var read = await reader.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            var remaining = maxCapturedOutputCharacters - captured;
            if (remaining > 0)
            {
                var toAppend = Math.Min(read, remaining);
                builder.Append(buffer, 0, toAppend);
                captured += toAppend;
            }

            if (read > remaining)
            {
                truncated = true;
            }
        }

        return new CapturedOutput(builder.ToString().Trim(), truncated);
    }

    private static string FormatMessage(int exitCode, CapturedOutput stdout, CapturedOutput stderr)
    {
        var builder = new StringBuilder($"exit-code-{exitCode}");
        AppendOutput(builder, "stdout", stdout);
        AppendOutput(builder, "stderr", stderr);

        return builder.ToString();
    }

    private static void AppendOutput(StringBuilder builder, string name, CapturedOutput output)
    {
        if (output.Text.Length == 0 && !output.Truncated)
        {
            return;
        }

        builder.Append(' ');
        builder.Append(name);
        builder.Append(": ");
        builder.Append(output.Text);

        if (output.Truncated)
        {
            builder.Append(" [truncated]");
        }
    }

    private static void KillProcessTree(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
        }
    }

    private sealed record CapturedOutput(string Text, bool Truncated);
}
