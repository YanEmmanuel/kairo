namespace Kairo.Core.Models;

public readonly record struct FrameLayout(
    int TerminalWidth,
    int TerminalHeight,
    int ContentX,
    int ContentY,
    int ContentWidth,
    int ContentHeight,
    int HorizontalDensity,
    int VerticalDensity,
    int SourceX,
    int SourceY,
    int SourceWidth,
    int SourceHeight)
{
    public int ScaledWidth => ContentWidth * HorizontalDensity;

    public int ScaledHeight => ContentHeight * VerticalDensity;
}
