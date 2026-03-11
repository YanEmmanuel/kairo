using System.Globalization;
using Kairo.Core.Playback;
using Kairo.Core.Rendering;

namespace Kairo.Cli.Cli;

public static class CommandLineOptionsParser
{
    public static CommandLineParseResult Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
        {
            return new CommandLineParseResult(new PlaybackOptions { ShowHelp = true }, null);
        }

        string? inputPath = null;
        var mode = RenderModeKind.Auto;
        double? fps = null;
        var maxFps = false;
        int? width = null;
        int? height = null;
        var resizeMode = ResizeMode.Fit;
        var charset = " .,:;irsXA253hMHGS#9B&@";
        var invert = false;
        var color = true;
        var detail = DetailLevel.Balanced;
        var brightness = 0d;
        var contrast = 1d;
        var saturation = 1d;
        var gamma = 1d;
        var loop = false;
        var audio = AudioMode.Off;
        var benchmark = false;
        var stats = false;
        var noDiff = false;
        var fullRedraw = false;
        int? threads = null;
        int? bufferSize = null;
        var emojiStyle = "meme";
        var dither = DitherMode.None;
        var profile = false;
        string? saveFrames = null;
        string? exportGif = null;
        string? exportVideo = null;
        var previewFrame = false;
        var startAt = TimeSpan.Zero;
        TimeSpan? duration = null;
        var showHelp = false;
        var showVersion = false;

        for (var index = 0; index < args.Count; index++)
        {
            var arg = args[index];

            if (!arg.StartsWith('-'))
            {
                if (string.IsNullOrWhiteSpace(inputPath))
                {
                    inputPath = arg;
                    continue;
                }

                return new CommandLineParseResult(null, $"Unexpected positional argument '{arg}'.");
            }

            switch (arg)
            {
                case "-h":
                case "--help":
                    showHelp = true;
                    break;
                case "--version":
                    showVersion = true;
                    break;
                case "--mode":
                    if (!TryReadValue(args, ref index, out var modeValue))
                    {
                        return MissingValue(arg);
                    }

                    if (!TryParseMode(modeValue, out mode))
                    {
                        return new CommandLineParseResult(null, $"Unsupported mode '{modeValue}'.");
                    }

                    break;
                case "--fps":
                    if (!TryReadValue(args, ref index, out var fpsValue))
                    {
                        return MissingValue(arg);
                    }

                    if (!double.TryParse(fpsValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedFps) || parsedFps <= 0d)
                    {
                        return new CommandLineParseResult(null, $"Invalid FPS value '{fpsValue}'.");
                    }

                    fps = parsedFps;
                    break;
                case "--max-fps":
                    maxFps = true;
                    break;
                case "--width":
                    if (!TryReadPositiveInt(args, ref index, arg, out width, out var widthError))
                    {
                        return widthError;
                    }

                    break;
                case "--height":
                    if (!TryReadPositiveInt(args, ref index, arg, out height, out var heightError))
                    {
                        return heightError;
                    }

                    break;
                case "--fit":
                    resizeMode = ResizeMode.Fit;
                    break;
                case "--crop":
                    resizeMode = ResizeMode.Crop;
                    break;
                case "--stretch":
                    resizeMode = ResizeMode.Stretch;
                    break;
                case "--charset":
                    if (!TryReadValue(args, ref index, out charset))
                    {
                        return MissingValue(arg);
                    }

                    break;
                case "--invert":
                    invert = true;
                    break;
                case "--color":
                    color = true;
                    break;
                case "--no-color":
                    color = false;
                    break;
                case "--detail":
                    if (!TryReadValue(args, ref index, out var detailValue))
                    {
                        return MissingValue(arg);
                    }

                    if (!Enum.TryParse(detailValue, true, out detail))
                    {
                        return new CommandLineParseResult(null, $"Unsupported detail level '{detailValue}'.");
                    }

                    break;
                case "--brightness":
                    if (!TryReadDouble(args, ref index, arg, out brightness, out var brightnessError))
                    {
                        return brightnessError;
                    }

                    break;
                case "--contrast":
                    if (!TryReadDouble(args, ref index, arg, out contrast, out var contrastError))
                    {
                        return contrastError;
                    }

                    break;
                case "--saturation":
                    if (!TryReadDouble(args, ref index, arg, out saturation, out var saturationError))
                    {
                        return saturationError;
                    }

                    break;
                case "--gamma":
                    if (!TryReadDouble(args, ref index, arg, out gamma, out var gammaError))
                    {
                        return gammaError;
                    }

                    break;
                case "--loop":
                    loop = true;
                    break;
                case "--audio":
                    if (!TryReadValue(args, ref index, out var audioValue))
                    {
                        return MissingValue(arg);
                    }

                    if (!TryParseAudioMode(audioValue, out audio))
                    {
                        return new CommandLineParseResult(null, $"Unsupported audio mode '{audioValue}'.");
                    }

                    break;
                case "--mute":
                    audio = AudioMode.Off;
                    break;
                case "--benchmark":
                    benchmark = true;
                    break;
                case "--stats":
                    stats = true;
                    break;
                case "--no-diff":
                    noDiff = true;
                    break;
                case "--full-redraw":
                    fullRedraw = true;
                    break;
                case "--threads":
                    if (!TryReadValue(args, ref index, out var threadValue))
                    {
                        return MissingValue(arg);
                    }

                    if (string.Equals(threadValue, "auto", StringComparison.OrdinalIgnoreCase))
                    {
                        threads = null;
                        break;
                    }

                    if (!int.TryParse(threadValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedThreads) || parsedThreads <= 0)
                    {
                        return new CommandLineParseResult(null, $"Invalid thread count '{threadValue}'.");
                    }

                    threads = parsedThreads;
                    break;
                case "--buffer-size":
                    if (!TryReadPositiveInt(args, ref index, arg, out bufferSize, out var bufferError))
                    {
                        return bufferError;
                    }

                    break;
                case "--emoji-style":
                    if (!TryReadValue(args, ref index, out emojiStyle))
                    {
                        return MissingValue(arg);
                    }

                    break;
                case "--dither":
                    if (!TryReadValue(args, ref index, out var ditherValue))
                    {
                        return MissingValue(arg);
                    }

                    if (!Enum.TryParse(ditherValue, true, out dither))
                    {
                        return new CommandLineParseResult(null, $"Unsupported dither mode '{ditherValue}'.");
                    }

                    break;
                case "--profile":
                    profile = true;
                    break;
                case "--save-frames":
                    if (!TryReadValue(args, ref index, out saveFrames))
                    {
                        return MissingValue(arg);
                    }

                    break;
                case "--export-gif":
                    if (!TryReadValue(args, ref index, out exportGif))
                    {
                        return MissingValue(arg);
                    }

                    break;
                case "--export-video":
                    if (!TryReadValue(args, ref index, out exportVideo))
                    {
                        return MissingValue(arg);
                    }

                    break;
                case "--preview-frame":
                    previewFrame = true;
                    break;
                case "--start-at":
                    if (!TryReadTimeSpan(args, ref index, arg, out startAt, out var startError))
                    {
                        return startError;
                    }

                    break;
                case "--duration":
                    if (!TryReadTimeSpan(args, ref index, arg, out var parsedDuration, out var durationError))
                    {
                        return durationError;
                    }

                    duration = parsedDuration;
                    break;
                default:
                    return new CommandLineParseResult(null, $"Unknown option '{arg}'.");
            }
        }

        if (!showHelp && !showVersion && string.IsNullOrWhiteSpace(inputPath))
        {
            return new CommandLineParseResult(null, "An input path or URL is required.");
        }

        return new CommandLineParseResult(
            new PlaybackOptions
            {
                InputPath = inputPath ?? string.Empty,
                Mode = mode,
                Fps = fps,
                MaxFps = maxFps,
                Width = width,
                Height = height,
                ResizeMode = resizeMode,
                Charset = charset,
                InvertCharset = invert,
                ColorEnabled = color,
                Detail = detail,
                Brightness = brightness,
                Contrast = contrast,
                Saturation = saturation,
                Gamma = gamma,
                Loop = loop,
                Audio = audio,
                Benchmark = benchmark,
                Stats = stats,
                DisableDiff = noDiff,
                ForceFullRedraw = fullRedraw,
                Threads = threads,
                BufferSize = bufferSize,
                EmojiStyle = emojiStyle,
                Dither = dither,
                Profile = profile,
                SaveFramesPath = saveFrames,
                ExportGifPath = exportGif,
                ExportVideoPath = exportVideo,
                PreviewFrame = previewFrame,
                StartAt = startAt,
                Duration = duration,
                ShowHelp = showHelp,
                ShowVersion = showVersion
            },
            null);
    }

    private static CommandLineParseResult MissingValue(string option) =>
        new(null, $"Missing value for option '{option}'.");

    private static bool TryReadValue(IReadOnlyList<string> args, ref int index, out string value)
    {
        if (index + 1 >= args.Count)
        {
            value = string.Empty;
            return false;
        }

        value = args[++index];
        return true;
    }

    private static bool TryReadPositiveInt(
        IReadOnlyList<string> args,
        ref int index,
        string option,
        out int? value,
        out CommandLineParseResult error)
    {
        if (!TryReadValue(args, ref index, out var raw))
        {
            value = null;
            error = MissingValue(option);
            return false;
        }

        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
        {
            value = null;
            error = new CommandLineParseResult(null, $"Invalid numeric value '{raw}' for option '{option}'.");
            return false;
        }

        value = parsed;
        error = new CommandLineParseResult(null, null);
        return true;
    }

    private static bool TryReadDouble(
        IReadOnlyList<string> args,
        ref int index,
        string option,
        out double value,
        out CommandLineParseResult error)
    {
        if (!TryReadValue(args, ref index, out var raw))
        {
            value = default;
            error = MissingValue(option);
            return false;
        }

        if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            error = new CommandLineParseResult(null, $"Invalid numeric value '{raw}' for option '{option}'.");
            return false;
        }

        error = new CommandLineParseResult(null, null);
        return true;
    }

    private static bool TryReadTimeSpan(
        IReadOnlyList<string> args,
        ref int index,
        string option,
        out TimeSpan value,
        out CommandLineParseResult error)
    {
        if (!TryReadValue(args, ref index, out var raw))
        {
            value = default;
            error = MissingValue(option);
            return false;
        }

        if (!raw.Contains(':') &&
            double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) &&
            seconds >= 0d)
        {
            value = TimeSpan.FromSeconds(seconds);
            error = new CommandLineParseResult(null, null);
            return true;
        }

        if (TimeSpan.TryParse(raw, CultureInfo.InvariantCulture, out value))
        {
            error = new CommandLineParseResult(null, null);
            return true;
        }

        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds) && seconds >= 0d)
        {
            value = TimeSpan.FromSeconds(seconds);
            error = new CommandLineParseResult(null, null);
            return true;
        }

        error = new CommandLineParseResult(null, $"Invalid time value '{raw}' for option '{option}'.");
        return false;
    }

    private static bool TryParseMode(string value, out RenderModeKind kind)
    {
        kind = value.ToLowerInvariant() switch
        {
            "auto" => RenderModeKind.Auto,
            "ascii" => RenderModeKind.Ascii,
            "blocks" => RenderModeKind.Blocks,
            "braille" => RenderModeKind.Braille,
            "emoji" => RenderModeKind.Emoji,
            _ => default
        };

        return kind != default || string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseAudioMode(string value, out AudioMode mode)
    {
        switch (value.ToLowerInvariant())
        {
            case "on":
            case "true":
            case "1":
                mode = AudioMode.On;
                return true;
            case "off":
            case "false":
            case "0":
                mode = AudioMode.Off;
                return true;
            default:
                mode = default;
                return false;
        }
    }
}
