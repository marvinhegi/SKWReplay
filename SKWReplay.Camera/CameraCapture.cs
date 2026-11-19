using OpenCvSharp;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SKWReplay.Camera;

public sealed record CameraFrame(Mat Image, DateTimeOffset Timestamp);

public sealed class CameraCapture : IDisposable
{
    public event Action<CameraFrame>? FrameReceived;
    public event Action<string>? ErrorOccurred;

    private VideoCapture? _capture;
    private CancellationTokenSource? _stop;
    private Task? _task;
    private readonly object _sync = new();

    public double Zoom { get; set; } = 1.0;
    public double PanX { get; set; }
    public double PanY { get; set; }

    public bool Start(int cameraIndex)
    {
        Stop();
        try
        {
            _capture = new VideoCapture(cameraIndex, VideoCaptureAPIs.DSHOW);
            _capture.Set(VideoCaptureProperties.FrameWidth, 1920);
            _capture.Set(VideoCaptureProperties.FrameHeight, 1080);
            if (!_capture.IsOpened())
            {
                ErrorOccurred?.Invoke("Die Kamera konnte nicht geöffnet werden. Sie wird eventuell von einer anderen Anwendung verwendet.");
                return false;
            }

            _stop = new CancellationTokenSource();
            _task = Task.Run(() => CaptureLoop(_stop.Token));
            return true;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex.Message);
            return false;
        }
    }

    private void CaptureLoop(CancellationToken token)
    {
        using var mat = new Mat();
        var stopwatch = Stopwatch.StartNew();
        while (!token.IsCancellationRequested)
        {
            if (_capture is null || !_capture.Read(mat) || mat.Empty())
            {
                if (!token.IsCancellationRequested)
                    ErrorOccurred?.Invoke("Die Kamera wurde unterbrochen oder getrennt.");
                break;
            }

            lock (_sync)
            {
                Mat output = ApplyZoom(mat);
                FrameReceived?.Invoke(new CameraFrame(output, DateTimeOffset.UtcNow));
            }
            Thread.Sleep(8);
        }
    }

    private Mat ApplyZoom(Mat source)
    {
        if (Zoom <= 1.01)
            return source.Clone();

        double zoom = Math.Clamp(Zoom, 1.0, 4.0);
        int cropWidth = (int)(source.Width / zoom);
        int cropHeight = (int)(source.Height / zoom);
        int centerX = source.Width / 2 + (int)(PanX * source.Width);
        int centerY = source.Height / 2 + (int)(PanY * source.Height);
        int x = Math.Clamp(centerX - cropWidth / 2, 0, source.Width - cropWidth);
        int y = Math.Clamp(centerY - cropHeight / 2, 0, source.Height - cropHeight);

        using var roi = new Mat(source, new OpenCvSharp.Rect(x, y, cropWidth, cropHeight));
        var result = new Mat();
        Cv2.Resize(roi, result, new OpenCvSharp.Size(source.Width, source.Height), 0, 0, InterpolationFlags.Linear);
        return result;
    }

    public void Stop()
    {
        _stop?.Cancel();
        try { _task?.Wait(TimeSpan.FromSeconds(2)); } catch { }
        _capture?.Release();
        _capture?.Dispose();
        _capture = null;
        _stop = null;
    }

    public void Dispose() => Stop();
}
