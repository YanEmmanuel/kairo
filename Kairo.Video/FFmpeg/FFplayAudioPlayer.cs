using System.Diagnostics;
using Kairo.Core.Contracts;
using Kairo.Core.Playback;

namespace Kairo.Video.FFmpeg;

public sealed class FFplayAudioPlayer : IAudioPlayer
{
    public async Task<IAsyncDisposable> StartAsync(AudioPlaybackRequest request, CancellationToken cancellationToken)
    {
        Process process;

        try
        {
            var startInfo = ExternalProcessStartInfoFactory.Create(
                "ffplay",
                FFplayArgumentBuilder.BuildAudioPlayback(request),
                redirectStandardOutput: true,
                redirectStandardError: true);

            process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start ffplay.");
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidOperationException(
                "Audio playback requires the bundled 'ffplay' binary or a system 'ffplay' in PATH. Use the portable release bundle or run with '--audio off'.",
                exception);
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        var session = new FFplayAudioSession(process, stdoutTask, stderrTask, cancellationToken);

        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(150), cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await session.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        if (process.HasExited && process.ExitCode != 0)
        {
            var stderr = await stderrTask.ConfigureAwait(false);
            await session.DisposeAsync().ConfigureAwait(false);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr) ? "ffplay exited with an error." : stderr.Trim());
        }

        return session;
    }

    private sealed class FFplayAudioSession : IAsyncDisposable
    {
        private readonly Process _process;
        private readonly Task<string> _stdoutTask;
        private readonly Task<string> _stderrTask;
        private readonly CancellationTokenRegistration _registration;

        public FFplayAudioSession(
            Process process,
            Task<string> stdoutTask,
            Task<string> stderrTask,
            CancellationToken cancellationToken)
        {
            _process = process;
            _stdoutTask = stdoutTask;
            _stderrTask = stderrTask;
            _registration = cancellationToken.Register(static state =>
            {
                if (state is Process running && !running.HasExited)
                {
                    try
                    {
                        running.Kill(entireProcessTree: true);
                    }
                    catch
                    {
                    }
                }
            }, process);
        }

        public async ValueTask DisposeAsync()
        {
            _registration.Dispose();

            if (!_process.HasExited)
            {
                try
                {
                    _process.Kill(entireProcessTree: true);
                }
                catch
                {
                }
            }

            try
            {
                await _process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
            }

            try
            {
                await _stdoutTask.ConfigureAwait(false);
            }
            catch
            {
            }

            try
            {
                await _stderrTask.ConfigureAwait(false);
            }
            catch
            {
            }

            _process.Dispose();
        }
    }
}
