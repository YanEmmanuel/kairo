using Kairo.Cli.Cli;
using Kairo.Core.Playback;
using Kairo.Rendering.Pipeline;
using Kairo.Terminal.Ansi;
using Kairo.Video.FFmpeg;

var parseResult = CommandLineOptionsParser.Parse(args);
if (parseResult.HasError)
{
    Console.Error.WriteLine(parseResult.ErrorMessage);
    Console.Error.WriteLine();
    Console.Error.WriteLine(HelpText.Build());
    return 1;
}

var options = parseResult.Options ?? new PlaybackOptions { ShowHelp = true };

if (options.ShowHelp)
{
    Console.WriteLine(HelpText.Build());
    return 0;
}

if (options.ShowVersion)
{
    Console.WriteLine("Kairo 0.1.0");
    return 0;
}

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cts.Cancel();
};

try
{
    var inputResolver = new YtDlpMediaInputResolver();
    if (YtDlpMediaInputResolver.LooksLikeRemoteInput(options.InputPath))
    {
        Console.Error.WriteLine("Resolving remote media with yt-dlp...");
    }

    var resolvedInput = await inputResolver.ResolveAsync(options.InputPath, cts.Token);
    options.ResolvedInputPath = resolvedInput.PlaybackPath;

    using var terminal = new StandardTerminalDevice();
    var player = new KairoPlayer(
        new FFmpegVideoProbe(),
        new FFmpegFrameSource(),
        terminal,
        RenderModeRegistry.CreateDefault(),
        new FFplayAudioPlayer());

    var snapshot = await player.PlayAsync(options, cts.Token);
    if (options.Stats || options.Benchmark)
    {
        Console.WriteLine();
        Console.WriteLine($"decoded:  {snapshot.DecodedFrames}");
        Console.WriteLine($"rendered: {snapshot.RenderedFrames}");
        Console.WriteLine($"dropped:  {snapshot.DroppedFrames}");
        Console.WriteLine($"fps:      {snapshot.ActualFps:F2}");
        Console.WriteLine($"render:   {snapshot.AverageRenderMs:F2} ms/frame");
        Console.WriteLine($"output:   {snapshot.AverageOutputMs:F2} ms/frame");
        Console.WriteLine($"elapsed:  {snapshot.Elapsed}");
    }

    return 0;
}
catch (OperationCanceledException)
{
    return 130;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}
