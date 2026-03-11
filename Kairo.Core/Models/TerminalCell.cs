namespace Kairo.Core.Models;

public readonly record struct TerminalCell(char Glyph, Rgb24 Foreground, Rgb24 Background)
{
    public static TerminalCell Empty { get; } = new(' ', Rgb24.Black, Rgb24.Black);
}
