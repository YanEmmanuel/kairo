# Contributing

Kairo accepts issues, bug reports, performance investigations, documentation fixes, and code contributions.

## Before opening a PR

- Open an issue first for large changes, feature work, or architectural shifts.
- Keep PRs focused. Separate renderer changes, CLI changes, and packaging changes when possible.
- Prefer fixing the root cause over adding flags or special cases.

## Development setup

Requirements:

- .NET SDK 10.0
- `ffmpeg`
- `ffprobe`
- `yt-dlp` for URL testing
- `ffplay` for audio testing

Common commands:

```bash
dotnet build Kairo.slnx
dotnet test Kairo.Tests/Kairo.Tests.csproj
dotnet run --project Kairo.Cli -- video.mp4
```

## Engineering expectations

- Preserve the current terminal-first architecture and keep the hot path small.
- Prefer low-allocation changes in decode, render, diff, and output code.
- Avoid unrelated refactors in feature PRs.
- Add or update tests when changing CLI parsing, layout logic, playback planning, or render behavior.
- Keep documentation aligned with the real state of the project.

## Pull request checklist

- The project builds locally.
- Tests pass locally.
- New flags or behavior are reflected in `README.md`.
- New release or packaging behavior is reflected in `.github/workflows/` when relevant.
- Breaking changes are called out clearly in the PR description.

## Style notes

- Use concise names and keep code ASCII unless the file already depends on Unicode glyphs.
- Prefer pragmatic comments over commentary-heavy code.
- Match existing project patterns unless there is a clear technical reason to change them.

## Contribution license

By submitting a contribution, you agree that your contribution will be licensed under the Apache License 2.0 used by this repository.
