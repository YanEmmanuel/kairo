using System.Threading.Channels;
using Kairo.Core.Models;
using Kairo.Core.Playback;

namespace Kairo.Core.Contracts;

public interface IFrameSource
{
    Task ProduceAsync(FrameSourceRequest request, ChannelWriter<VideoFrame> writer, CancellationToken cancellationToken);
}
