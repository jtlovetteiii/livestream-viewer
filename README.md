# livestream-viewer
Self-contained passive live-stream viewer with support for off-air and off-line static videos.

# Use-Case
This application was designed to solve a specific use-case: continuously displaying a live video feed on a display device. It does so by periodically testing a configured live-stream URL for frame data, then displaying either the stream (if the test succeeded), a configured "Off-Air" video (if the test failed but Internet connectivity is available), or an "Offline" video (if the test failed and Internet connectivity is unavailable). To ensure maximum compatibility across platforms and devices, this application relies on simple, no-frills video players such as FFPLAY and OMXPLAYER.

# Workflow
```
+---------+                   +---------+                              +-------+ +---------+
| Monitor |                   | Viewer  |                              | Video | | Stream  |
+---------+                   +---------+                              +-------+ +---------+
     |                             |                                       |          |
     | Request frames.             |                                       |          |
     |------------------------------------------------------------------------------->|
     |                             |                                       |          |
     |                             |     frame001.jpg, frame002.jpg, ... framexxx.jpg |
     |<-------------------------------------------------------------------------------|
     |                             |                                       |          |
     | Activate video player.      |                                       |          |
     |---------------------------->|                                       |          |
     |                             |                                       |          |
     |                             | (if frames) Play stream.              |          |
     |                             |------------------------------------------------->|
     |                             |                                       |          |
     |                             | (if no frames) Loop OffAir.mp4.       |          |
     |                             |-------------------------------------->|          |
     |                             |                                       |          |
     |                             | (if no Internet) Loop Offline.mp4     |          |
     |                             |-------------------------------------->|          |
     |                             |                                       |          |
```
*Generated with textart.io/sequence using the below text:*
```
object Monitor Viewer Video Stream
Monitor->Stream: Request frames.
Stream->Monitor: frame001.jpg, frame002.jpg, ... framexxx.jpg
Monitor->Viewer: Activate video player.
Viewer->Stream: (if frames) Play stream.
Viewer->Video: (if no frames) Loop OffAir.mp4.
Viewer->Video: (if no Internet) Loop Offline.mp4
```
The application is largely concerned with orchestration of two primary components: a Monitor and a Viewer. Periodically, the Monitor will attempt to read sequential frames from a livestream URL and store them in the working directory. If any frames are downloaded, then the Monitor requests that the Viewer activate a video player using the livestream URL as input. If no frames are downloaded, then the Monitor performs an Internet connectivity test. If the test succeeds, then the livestream is considered to be "off-air", and the Monitor requests that the Viewer activate a video player using the 

# Technology
This application is built with .NET Core to maximize cross-platform support. It relies exclusively on platform-specific video players to test live-stream availability (FFMPEG) and display live and static video content (FFPLAY on Windows and OMXPLAYER on Linux).

# Supported Stream Types
Currently, only RTMP streams have been tested. However, any streaming protocol supported by FFPLAY and OMXPLAYER is theoretically supported.

# Shortcomings
Transitions between videos are not seamless: there is a "gap" between when one video stops and the next one starts. This might could be avoided by allowing overlapping instances of the video player (e.g. when stopping the "Off Air" video to display the live-stream), but this would be unreliable, as it is currently impossible to determine precisely when enough of the live-stream has been buffered to begin displaying it. If the overlap is too short, then the gap will still be visible, and if the overlap is too long, then the two instances will compete for the display, leading to an undesirable "jerking" effect as frames alternate between the two videos.

# Build

# Custom Videos

# Configuration

# Environments
