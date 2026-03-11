using Kairo.Core.Models;

namespace Kairo.Core.Contracts;

public interface IVideoProbe
{
    Task<VideoMetadata> ProbeAsync(string inputPath, CancellationToken cancellationToken);
}
