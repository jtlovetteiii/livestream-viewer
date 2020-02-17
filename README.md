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
The application is largely concerned with orchestration of two primary components: a Monitor and a Viewer. Periodically, the Monitor will attempt to read sequential frames from a livestream URL and store them in the working directory. If any frames are downloaded, then the Monitor requests that the Viewer activate a video player using the livestream URL as input. If no frames are downloaded, then the Monitor performs an Internet connectivity test. If the test succeeds, then the livestream is considered to be "off-air", and the Monitor requests that the Viewer play (looping continuously) a video named "OffAir.mp4". If the test fails, then the device is considered to be offline, and the Monitor requests that the Viewer play (looping continuously) a video named "Offline.mp4"). This process repeats as long as the program is running.

# Technology
This application is built with .NET Core to maximize cross-platform support. It relies exclusively on platform-specific video players to test live-stream availability (FFMPEG) and display live and static video content (FFPLAY on Windows and OMXPLAYER on Linux).

# Supported Stream Types
Currently, only RTMP streams have been tested. However, any streaming protocol supported by FFPLAY and OMXPLAYER is theoretically supported.

# Shortcomings
The following items are known limitations/shortcomings of the current implementation. Future revisions may mitigate them.

## Delayed Transitions
Transitions between videos are not seamless: there is a "gap" between when one video stops and the next one starts. This might could be avoided by allowing overlapping instances of the video player (e.g. when stopping the "Off Air" video to display the live-stream), but this would be unreliable, as it is currently impossible to determine precisely when enough of the live-stream has been buffered to begin displaying it. If the overlap is too short, then the gap will still be visible, and if the overlap is too long, then the two instances will compete for the display, leading to an undesirable "jerking" effect as frames alternate between the two videos.

## Lack of Display Guarantee
As currently implemented, the application has no ability to verify that videos are actually being displayed (largely due to reliance upon external video players). For instance, OMXPLAYER sometimes fails to show live video, instead merely reporting the codec in a terminal display. While rare, this application currently has no mechanism to detect such a scenario, meaning that, until the next transition, the device will likely display either a terminal window or the underlying desktop GUI. Future revisions may handle this situation by testing pixels from the screen buffer or by using alternate video playback technology. For now, it is recommended that this application be at least periodically restarted to ensure that, if a video player ever enters this state, there will at least be some chance of recovery.

# Distributions
Pre-generated builds of the application can be found

# Build
If you make changes to the application and want to produce your own builds and distributions, start by making edits in your IDE of choice (Visual Studio Community Edition is a great place to start if you're unsure), then follow the below procedure.

Navigate to src\LivestreamViewer, then execute the following command:

`dotnet build LivestreamViewer.csproj`

This will compile the application, placing the binaries in src\LivestreamViewer\bin\Debug\netcoreapp2.1

While in the same folder (src\LivestreamViewer), execute this command to generate a platform-specific deployment package:

`dotnet publish --runtime xxx --self-contained`

Replace "xxx" with a [Runtime Identifier](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog); common values are `win-x86` for 32-bit Windows and `linux-arm` for Raspberry Pi.

*Refer to the [.NET Core publish documentation](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-publish) for information about available options, including "framework-dependent" and "standalone" builds.*

The `dotnet publish` command will generate platform-specific executables (including the .NET Core runtime) in src\LivestreamViewer\bin\Debug\netcoreapp2.1\xxx (where "xxx" is the Runtime Identifier). These files can be deployed to and executed on another computer with or without the .NET Core runtime.

# Configuration and Convention

# Custom Videos

# Examples

## Windows

## Linux
