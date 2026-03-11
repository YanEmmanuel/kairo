namespace Kairo.Rendering.Pipeline;

public sealed class LuminanceCharacterMapper
{
    private readonly char[] _characters;

    public LuminanceCharacterMapper(string charset, bool invert)
    {
        if (string.IsNullOrWhiteSpace(charset))
        {
            throw new ArgumentException("Charset cannot be empty.", nameof(charset));
        }

        _characters = invert ? charset.Reverse().ToArray() : charset.ToCharArray();
    }

    public char Map(double luminance, int x = 0, int y = 0, bool useBayer = false)
    {
        var adjusted = useBayer ? BayerDither.Apply(luminance, x, y, _characters.Length) : luminance;
        var index = (int)Math.Round(adjusted * (_characters.Length - 1), MidpointRounding.AwayFromZero);
        return _characters[Math.Clamp(index, 0, _characters.Length - 1)];
    }
}
