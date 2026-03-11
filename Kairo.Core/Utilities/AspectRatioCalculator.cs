using Kairo.Core.Models;
using Kairo.Core.Playback;
using Kairo.Core.Rendering;

namespace Kairo.Core.Utilities;

public static class AspectRatioCalculator
{
    public static FrameLayout Calculate(
        VideoMetadata metadata,
        TerminalSize terminal,
        PlaybackOptions options,
        ModeDescriptor descriptor)
    {
        var terminalWidth = Math.Max(1, options.Width ?? terminal.Width);
        var terminalHeight = Math.Max(1, options.Height ?? terminal.Height);
        var targetAspect = terminalWidth * terminal.CellAspectRatio / terminalHeight;
        var sourceAspect = metadata.Width / (double)metadata.Height;

        var contentWidth = terminalWidth;
        var contentHeight = terminalHeight;
        var sourceX = 0;
        var sourceY = 0;
        var sourceWidth = metadata.Width;
        var sourceHeight = metadata.Height;

        switch (options.ResizeMode)
        {
            case ResizeMode.Fit:
                if (sourceAspect > targetAspect)
                {
                    contentHeight = Math.Max(1, (int)Math.Round(terminalWidth * terminal.CellAspectRatio / sourceAspect));
                }
                else
                {
                    contentWidth = Math.Max(1, (int)Math.Round(terminalHeight * sourceAspect / terminal.CellAspectRatio));
                }

                break;

            case ResizeMode.Crop:
                if (sourceAspect > targetAspect)
                {
                    sourceWidth = Math.Max(1, (int)Math.Round(sourceHeight * targetAspect));
                    sourceX = (metadata.Width - sourceWidth) / 2;
                }
                else
                {
                    sourceHeight = Math.Max(1, (int)Math.Round(sourceWidth / targetAspect));
                    sourceY = (metadata.Height - sourceHeight) / 2;
                }

                break;
        }

        var contentX = Math.Max(0, (terminalWidth - contentWidth) / 2);
        var contentY = Math.Max(0, (terminalHeight - contentHeight) / 2);

        return new FrameLayout(
            terminalWidth,
            terminalHeight,
            contentX,
            contentY,
            contentWidth,
            contentHeight,
            descriptor.HorizontalDensity,
            descriptor.VerticalDensity,
            sourceX,
            sourceY,
            sourceWidth,
            sourceHeight);
    }
}
