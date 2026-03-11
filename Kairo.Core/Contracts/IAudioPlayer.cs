using Kairo.Core.Playback;

namespace Kairo.Core.Contracts;

public interface IAudioPlayer
{
    Task<IAsyncDisposable> StartAsync(AudioPlaybackRequest request, CancellationToken cancellationToken);
}
