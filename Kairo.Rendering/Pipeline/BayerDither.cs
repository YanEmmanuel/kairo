namespace Kairo.Rendering.Pipeline;

public static class BayerDither
{
    private static readonly double[,] Matrix4x4 =
    {
        { 0d / 16d, 8d / 16d, 2d / 16d, 10d / 16d },
        { 12d / 16d, 4d / 16d, 14d / 16d, 6d / 16d },
        { 3d / 16d, 11d / 16d, 1d / 16d, 9d / 16d },
        { 15d / 16d, 7d / 16d, 13d / 16d, 5d / 16d }
    };

    public static double Apply(double luminance, int x, int y, int levels)
    {
        if (levels <= 1)
        {
            return luminance;
        }

        var threshold = Matrix4x4[y & 3, x & 3] - 0.5d;
        var delta = threshold / levels;
        return Math.Clamp(luminance + delta, 0d, 1d);
    }
}
