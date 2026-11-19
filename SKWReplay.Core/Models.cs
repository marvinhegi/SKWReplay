namespace SKWReplay.Core;

public enum RecordingState
{
    Ready,
    Initializing,
    Recording,
    Paused,
    Buffering,
    Processing,
    Finished,
    Error,
    Saved
}

public sealed record CameraDevice(int Index, string Name);
public sealed record MicrophoneDevice(int Index, string Name);
