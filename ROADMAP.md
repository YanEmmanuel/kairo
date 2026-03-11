# Kairo Roadmap

## Near term

- Harden the default autoplay path for longer sessions
- Improve frame scheduling and lag compensation for real-world anime and film content
- Add exact per-frame timestamp support for VFR inputs
- Improve braille thresholding and perceptual mapping
- Add better monochrome behavior for `--no-color`
- Add richer live stats and profile output

## Visual modes

- `anime tuned`
  Higher edge clarity, color bias control, line-preserving defaults
- `cinema`
  Better dark-scene handling, contrast shaping, smoother detail mapping
- `ultra color`
  More aggressive color preservation and perceptual tuning
- `emoji`
  Real emoji palette search with multiple styles and fidelity strategies

## Inputs

- webcam
- live streams
- stdin pipe input
- alternate backends beyond external FFmpeg
- playlist/queue handling for remote inputs
- download cache policy and offline pinning

## Playback features

- subtitle overlay
- adaptive mode switching based on performance
- content-specific presets
- multi-pass tuning for static scenes
- richer audio controls and device selection

## Export

- frame dump export
- animated text export
- GIF export
- rendered video export

## Performance work

- lower-overhead terminal writer
- more aggressive diff coalescing
- optional dirty-rect tracking
- restart-free resize retargeting
- adaptive buffer sizing
- richer profiling hooks

## Long term

- true terminal player polish for long-form viewing
- adaptive renderer selection by terminal and hardware
- alternate decode backends
- distributed or accelerated render paths
