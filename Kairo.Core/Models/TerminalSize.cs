namespace Kairo.Core.Models;

public readonly record struct TerminalSize(int Width, int Height, double CellAspectRatio = 0.5d)
{
    public int Area => Width * Height;
}
