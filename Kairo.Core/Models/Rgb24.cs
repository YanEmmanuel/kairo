namespace Kairo.Core.Models;

public readonly record struct Rgb24(byte R, byte G, byte B)
{
    public static Rgb24 Black { get; } = new(0, 0, 0);

    public static Rgb24 White { get; } = new(255, 255, 255);

    public double Luminance =>
        ((0.2126d * R) + (0.7152d * G) + (0.0722d * B)) / 255d;

    public static Rgb24 Average(ReadOnlySpan<Rgb24> colors)
    {
        if (colors.IsEmpty)
        {
            return Black;
        }

        var red = 0;
        var green = 0;
        var blue = 0;

        foreach (var color in colors)
        {
            red += color.R;
            green += color.G;
            blue += color.B;
        }

        return new Rgb24(
            (byte)(red / colors.Length),
            (byte)(green / colors.Length),
            (byte)(blue / colors.Length));
    }
}
