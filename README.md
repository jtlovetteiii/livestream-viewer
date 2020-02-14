# livestream-viewer
Self-contained passive live-stream viewer with support for off-air and off-line static videos.

# Use-Case
This application was designed to solve a specific use-case: continuously displaying a live video feed on a display device. It does so by periodically testing a configured live-stream URL for frame data, then displaying either the stream (if the test succeeded), a configured "Off-Air" video (if the test failed but Internet connectivity is available), or an "Offline" video (if the test failed and Internet connectivity is unavailable). To ensure maximum compatibility across platforms and devices, this application relies on simple, no-frills video players such as FFPLAY and OMXPLAYER.

# Workflow

# Technology
This application is built with .NET Core to maximize cross-platform support. It relies exclusively on platform-specific video players to test live-stream availability (FFMPEG) and display live and static video content (FFPLAY on Windows and OMXPLAYER on Linux).

# Supported Stream Types
Currently, only RTMP streams have been tested. However, any streaming protocol supported by FFPLAY and OMXPLAYER is theoretically supported.

# Shortcomings
Transitions between videos are not seamless: there is a "gap" between when one video stops and the next one starts. This might could be avoided by allowing overlapping instances of the video player (e.g. when stopping the "Off Air" video to display the live-stream), but this would be unreliable, as it is currently impossible to determine precisely when enough of the live-stream has been buffered to begin displaying it. If the overlap is too short, then the gap will still be visible, and if the overlap is too long, then the two instances will compete for the display, leading to an undesirable "jerking" effect as frames alternate between the two videos.

# Build

# Configuration

# Environments
