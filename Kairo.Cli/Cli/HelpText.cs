namespace Kairo.Cli.Cli;

public static class HelpText
{
    public static string Build() =>
        """
        Kairo
        High-performance terminal video player with ANSI truecolor rendering, URL ingestion, and optional audio.

        Usage:
          kairo <path-or-url> [options]

        Core:
          kairo video.mp4
          kairo "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
          kairo "https://www.youtube.com/watch?v=dQw4w9WgXcQ" --audio on
          kairo video.mp4 --mode blocks --stats
          kairo video.mp4 --mode ascii --width 120 --height 40
          kairo video.mp4 --detail ultra --dither bayer

        Rendering:
          --mode ascii|blocks|braille|emoji
          --charset "<chars>"
          --invert
          --color
          --no-color
          --detail fast|balanced|quality|ultra|insane
          --dither none|bayer|floyd

        Layout:
          --width <cols>
          --height <rows>
          --fit
          --crop
          --stretch

        Playback:
          --fps <n>
          --max-fps
          --loop
          --audio on|off
          --preview-frame
          --start-at <seconds|hh:mm:ss>
          --duration <seconds|hh:mm:ss>
          --mute

        Tuning:
          --brightness <n>
          --contrast <n>
          --saturation <n>
          --gamma <n>
          --threads auto|<n>
          --buffer-size <n>
          --no-diff
          --full-redraw
          --benchmark
          --stats
          --profile

        Reserved for future iterations:
          --mode emoji
          --emoji-style <name>
          --save-frames <dir>
          --export-gif <path>
          --export-video <path>

        Notes:
          The default mode is auto and prefers truecolor block rendering.
          In blocks mode, detail quality and above enable denser 2x2 quadrant cells.
          Resize is tracked during playback when width or height is not fixed.
          FFmpeg and ffprobe must be available in PATH.
          URL playback requires yt-dlp in PATH.
          Audio playback requires ffplay in PATH.
        """;
}
