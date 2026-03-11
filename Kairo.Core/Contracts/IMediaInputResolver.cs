using Kairo.Core.Playback;

namespace Kairo.Core.Contracts;

public interface IMediaInputResolver
{
    Task<ResolvedMediaInput> ResolveAsync(string input, CancellationToken cancellationToken);
}
