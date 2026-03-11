# Kairo

Kairo is a serious terminal video player for animated textual rendering.

The goal is not to be a toy ASCII demo. The goal is to build a terminal-native playback engine that can grow into a real player: streaming decode, low-allocation rendering, ANSI truecolor output, incremental terminal updates, optional audio, remote ingestion, and a pipeline architecture that can absorb more aggressive modes over time.

The current build plays local files and remote URLs, can attach synchronized audio playback, renders frame-by-frame through FFmpeg, supports truecolor ANSI, uses diff rendering, exposes performance-oriented flags, and ships with tests and benchmarks.

## Why Kairo exists

Most terminal video experiments stop at novelty:

- full-screen redraw every frame
- low-fidelity grayscale output
- no serious decode pipeline
- no concern for allocation or sustained playback
- no architecture for future modes

Kairo starts from the opposite direction:

- stream frames instead of loading the whole file
- keep the hot path small
- push heavy scaling/crop work to FFmpeg
- render only terminal diffs when possible
- preserve color with 24-bit ANSI
- organize the codebase so new visual modes are additive, not invasive

## Current build

The current build includes:

- `kairo video.mp4`
- `kairo "https://www.youtube.com/watch?v=..."`
- FFmpeg-backed probe and frame streaming
- remote URL resolution and download through `yt-dlp`
- optional audio playback through `ffplay`
- frame-by-frame RGB decode via external `ffmpeg`
- automatic terminal sizing
- automatic default mode selection
- truecolor ANSI rendering
- useful visual modes:
  - `ascii`
  - `blocks`
  - `braille`
- diff rendering
- FPS control and frame dropping
- startup preroll for `--detail insane`
- resize-aware playback strategy
- CLI help
- unit tests
- benchmark project

## Solution layout

- `Kairo.Cli`
  CLI parsing, help text, tool entrypoint, runtime wiring
- `Kairo.Core`
  contracts, models, playback planning, presets, scheduler orchestration
- `Kairo.Rendering`
  visual modes and luminance/color mapping
- `Kairo.Video`
  FFmpeg probe and rawvideo frame streaming
- `Kairo.Terminal`
  ANSI writer, pooled buffer builder, diff renderer, terminal device
- `Kairo.Benchmarks`
  BenchmarkDotNet suite for render and ANSI hot paths
- `Kairo.Tests`
  unit tests for parser, mapping, aspect ratio, presets, diff rendering, resize behavior

## Architecture

Kairo is built around a pipeline mindset:

1. `frame source`
   `ffprobe` extracts metadata and `ffmpeg` streams RGB frames through stdout.
2. `preprocess`
   crop, scale, brightness, contrast, saturation, gamma, and optional FPS limiting are delegated to FFmpeg.
3. `mode transform`
   the selected mode converts the incoming scaled RGB frame into terminal cells.
4. `frame composition`
   renderers write into a reusable `TerminalFrameBuffer`.
5. `terminal diff renderer`
   only changed cells are encoded as ANSI when diff rendering is enabled.
6. `output scheduling`
   the player tracks timestamps, delays when needed, and drops frames if the terminal falls behind.

Important implementation choices:

- FFmpeg does the heavy spatial resampling. This keeps C# focused on composition instead of brute-force downscaling.
- Frames are streamed through a bounded `Channel<VideoFrame>` for producer/consumer flow.
- `VideoFrame` and terminal buffers are backed by `ArrayPool<T>` to avoid per-frame allocation churn.
- ANSI output is built through a reusable pooled char buffer instead of allocating a new giant string every frame.
- Resize is handled pragmatically: when auto-sized playback detects a terminal size change, Kairo restarts the FFmpeg stream from the current playback position with a new render plan.

## Requirements

- .NET SDK 10.0
- `ffmpeg`, `ffprobe`, `yt-dlp`, `ffplay`, and `deno` only if you are running the raw binary or building from source

This repository currently targets `net10.0` because that is the SDK available in the development environment used for this build.

## Build

```bash
dotnet build Kairo.slnx
```

## Releases

Push a tag like `v0.1.0` and GitHub Actions will publish:

- raw single-file binaries:
  - `kairo-linux-x64`
  - `kairo-win-x64.exe`
- portable plug-and-play bundles with embedded toolchain:
  - `kairo-linux-x64-portable.tar.gz`
  - `kairo-win-x64-portable.zip`

The portable bundles include `ffmpeg`, `ffprobe`, `ffplay`, `yt-dlp`, and `deno` so URL playback and audio work without any extra system installation after extraction.

## Run

The simplest form is:

```bash
dotnet run --project Kairo.Cli -- video.mp4
```

If you want the tool command name locally:

```bash
dotnet pack Kairo.Cli -c Release
dotnet tool install --global --add-source ./Kairo.Cli/bin/Release Kairo
```

After that:

```bash
kairo video.mp4
```

## Usage examples

```bash
kairo video.mp4
kairo "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
kairo "https://www.youtube.com/watch?v=dQw4w9WgXcQ" --audio on
kairo video.mp4 --mode ascii
kairo video.mp4 --mode blocks --stats
kairo video.mp4 --mode braille --detail ultra
kairo video.mp4 --width 120 --height 40 --fit
kairo video.mp4 --crop --brightness 0.05 --contrast 1.1 --saturation 1.15
kairo video.mp4 --fps 30
kairo video.mp4 --max-fps --benchmark
kairo video.mp4 --preview-frame --start-at 00:02:10
```

## CLI flags

Implemented:

- `--mode ascii|blocks|braille|emoji`
- `--fps`
- `--max-fps`
- `--width`
- `--height`
- `--fit`
- `--crop`
- `--stretch`
- `--charset`
- `--invert`
- `--color`
- `--no-color`
- `--detail fast|balanced|quality|ultra|insane`
- `--brightness`
- `--contrast`
- `--saturation`
- `--gamma`
- `--loop`
- `--audio on|off`
- `--mute`
- `--benchmark`
- `--stats`
- `--no-diff`
- `--full-redraw`
- `--threads auto|N`
- `--buffer-size`
- `--emoji-style`
- `--dither none|bayer|floyd`
- `--profile`
- `--save-frames`
- `--export-gif`
- `--export-video`
- `--preview-frame`
- `--start-at`
- `--duration`

Status notes:

- `emoji` mode is reserved but not implemented yet.
- `save-frames`, `export-gif`, and `export-video` are reserved but not implemented yet.
- `--mute` is kept as a compatibility shortcut for `--audio off`.
- URL inputs are downloaded into a local cache before playback so the same file can be reused by the video and audio pipelines.
- In `blocks` mode, `--detail quality`, `ultra`, and `insane` switch to denser 2x2 quadrant rendering.
- `--detail insane` now preloads extra frames before playback starts and, when audio is off, will slow playback slightly instead of dropping frames as soon as the terminal falls behind.

## Default behavior

The default path is intentionally opinionated:

- detect the terminal size
- keep color enabled
- choose `blocks` for general color playback
- prefer `braille` automatically on large terminals with aggressive detail presets
- use source FPS unless you override it
- keep diff rendering enabled

The default should be usable without twenty flags.

When the input is a remote URL, Kairo downloads it into `~/.cache/kairo/downloads` by default (or `$XDG_CACHE_HOME/kairo/downloads` when available) and reuses the resulting file for probe, frame decode, and optional audio playback.

## Testing

```bash
dotnet test Kairo.Tests/Kairo.Tests.csproj
```

Covered areas:

- luminance to character mapping
- ANSI color escape generation
- aspect ratio calculation
- diff rendering behavior
- CLI option parsing
- automatic preset choice
- resize tracking behavior

## Benchmarks

```bash
dotnet run --project Kairo.Benchmarks -c Release
```

Benchmark suite includes:

- ASCII vs blocks vs braille render transforms
- ANSI diff buffer assembly
- diff rendering vs full redraw
- cost changes across detail levels

## Current limitations

- variable frame rate content is scheduled from average FPS, not exact per-frame timestamps
- resize handling restarts the FFmpeg stream instead of dynamically retargeting an already-running decode graph
- no subtitle overlay yet
- no webcam/live/stdin ingestion yet
- no export path yet
- emoji mode is reserved but still pending

## Technical decisions worth noting

- External FFmpeg was chosen first for stability, leverage, and speed of delivery. It provides a strong baseline while keeping the architecture ready for an in-process decode backend later.
- `yt-dlp` is used only for remote URL normalization/download. Once resolved, Kairo works against a regular local file for probe, frame decode, and audio.
- `ffplay` is used as the audio backend so the video renderer can stay focused on terminal composition instead of audio device management.
- `blocks` is the default high-fidelity color mode because it gives materially better density than classic ASCII while still keeping per-cell composition cheap.
- `braille` exists as a denser mode already, but is not the default because it is more expensive and benefits more from larger terminals.
- Terminal output is written as raw ANSI sequences to stdout in the critical path. No heavy terminal UI framework sits in the render loop.

## Roadmap

See [ROADMAP.md](ROADMAP.md).

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for setup, review expectations, and PR guidelines.

## Smoke-tested workflow

The current build was validated locally with:

- `dotnet build Kairo.Cli/Kairo.Cli.csproj`
- `dotnet test Kairo.Tests/Kairo.Tests.csproj`
- `dotnet build Kairo.Benchmarks/Kairo.Benchmarks.csproj`
- `dotnet run --project Kairo.Cli -- /tmp/kairo-smoke.mp4 --preview-frame --width 40 --height 20 --mode blocks --stats`

## Credits

Created and maintained by Yan.

## License

Licensed under [Apache-2.0](LICENSE).
