using OpenCvSharp;
using System.Diagnostics;
using System.Globalization;

namespace SKWReplay.Recording;

public sealed class RecordingManager : IDisposable
{
    private readonly object _sync = new();
    private readonly List<string> _segments = new();
    private Process? _encoder;
    private BinaryWriter? _input;
    private int _width;
    private int _height;
    private bool _recording;
    private string _workingDirectory;

    public bool IsRecording => _recording;
    public string? LastError { get; private set; }

    public RecordingManager()
    {
        _workingDirectory = Path.Combine(Path.GetTempPath(), "SKWReplay");
        Directory.CreateDirectory(_workingDirectory);
    }

    public bool StartSegment(Mat firstFrame)
    {
        lock (_sync)
        {
            if (_recording) return true;
            try
            {
                string ffmpeg = FindFfmpeg();
                if (ffmpeg.Length == 0)
                {
                    LastError = "FFmpeg wurde nicht gefunden.";
                    return false;
                }

                _width = firstFrame.Width;
                _height = firstFrame.Height % 2 == 0 ? firstFrame.Height : firstFrame.Height - 1;
                if (_width % 2 != 0) _width--;
                string segment = Path.Combine(_workingDirectory, $"segment-{_segments.Count + 1:0000}-{Guid.NewGuid():N}.mp4");

                var arguments = $"-y -hide_banner -loglevel error -f rawvideo -pixel_format bgr24 -video_size {_width}x{_height} -framerate 30 -i pipe:0 -c:v libx264 -preset ultrafast -tune zerolatency -pix_fmt yuv420p -movflags +faststart \"{segment}\"";
                var psi = new ProcessStartInfo
                {
                    FileName = ffmpeg,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true
                };
                _encoder = Process.Start(psi);
                if (_encoder is null)
                {
                    LastError = "Der Video-Encoder konnte nicht gestartet werden.";
                    return false;
                }
                _encoder.ErrorDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data)) LastError = e.Data;
                };
                _encoder.BeginErrorReadLine();
                _input = new BinaryWriter(_encoder.StandardInput.BaseStream);
                _segments.Add(segment);
                _recording = true;
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }
    }

    public void WriteFrame(Mat frame)
    {
        lock (_sync)
        {
            if (!_recording || _input is null || _encoder is null || _encoder.HasExited) return;
            try
            {
                using var resized = new Mat();
                if (frame.Width != _width || frame.Height != _height)
                    Cv2.Resize(frame, resized, new OpenCvSharp.Size(_width, _height));
                else
                    frame.CopyTo(resized);
                long length = (long)resized.Step() * resized.Rows;
                byte[] bytes = GC.AllocateUninitializedArray<byte>((int)length);
                System.Runtime.InteropServices.Marshal.Copy(resized.Data, bytes, 0, bytes.Length);
                _input.Write(bytes);
                _input.Flush();
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
        }
    }

    public void Pause()
    {
        lock (_sync)
        {
            if (!_recording) return;
            FinishSegment();
        }
    }

    public void Stop()
    {
        lock (_sync)
        {
            if (_recording)
                FinishSegment();
        }
    }

    private void FinishSegment()
    {
        _recording = false;
        try
        {
            _input?.Dispose();
            _input = null;
            if (_encoder is not null)
            {
                _encoder.WaitForExit(15000);
                _encoder.Dispose();
                _encoder = null;
            }
        }
        catch (Exception ex) { LastError = ex.Message; }
    }

    public async Task<bool> SaveAsync(string destination, IProgress<int>? progress = null)
    {
        return await Task.Run(() =>
        {
            try
            {
                Stop();
                if (_segments.Count == 0)
                {
                    LastError = "Es wurde keine Aufnahme erstellt.";
                    return false;
                }

                progress?.Report(10);
                string ffmpeg = FindFfmpeg();
                if (ffmpeg.Length == 0)
                {
                    LastError = "FFmpeg wurde nicht gefunden.";
                    return false;
                }

                if (_segments.Count == 1)
                {
                    File.Copy(_segments[0], destination, true);
                    progress?.Report(100);
                    return true;
                }

                string list = Path.Combine(_workingDirectory, $"concat-{Guid.NewGuid():N}.txt");
                File.WriteAllLines(list, _segments.Select(s => $"file '{s.Replace("'", "'\\''")}'"));
                var psi = new ProcessStartInfo
                {
                    FileName = ffmpeg,
                    Arguments = $"-y -hide_banner -loglevel error -f concat -safe 0 -i \"{list}\" -c copy \"{destination}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(psi);
                if (process is null) { LastError = "FFmpeg konnte nicht gestartet werden."; return false; }
                process.WaitForExit();
                progress?.Report(process.ExitCode == 0 ? 100 : 50);
                if (process.ExitCode != 0)
                {
                    LastError = $"FFmpeg konnte das Video nicht zusammenführen. Exit-Code: {process.ExitCode}";
                    return false;
                }
                try { File.Delete(list); } catch { }
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        });
    }

    public void Cleanup()
    {
        Stop();
        foreach (string segment in _segments)
        {
            try { File.Delete(segment); } catch { }
        }
        _segments.Clear();
    }

    public static bool HasFreeDiskSpace(string path, ulong requiredBytes)
    {
        string fullPath = Path.GetFullPath(path);
        string root = Path.GetPathRoot(fullPath) ?? fullPath;
        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            if (root.StartsWith(drive.Name, StringComparison.OrdinalIgnoreCase))
                return (ulong)drive.AvailableFreeSpace >= requiredBytes;
        }
        return true;
    }

    private static string FindFfmpeg()
    {
        string[] candidates =
        {
            Path.Combine(AppContext.BaseDirectory, "ffmpeg", "ffmpeg.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Microsoft\WinGet\Links\ffmpeg.exe"),
            @"C:\ffmpeg\bin\ffmpeg.exe"
        };
        foreach (string candidate in candidates)
            if (File.Exists(candidate)) return candidate;
        foreach (string dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
        {
            string candidate = Path.Combine(dir.Trim('"'), "ffmpeg.exe");
            if (File.Exists(candidate)) return candidate;
        }
        return "";
    }

    public void Dispose()
    {
        Cleanup();
    }
}
