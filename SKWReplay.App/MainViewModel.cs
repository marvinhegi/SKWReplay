using Microsoft.Win32;
using OpenCvSharp;
using SKWReplay.Core;
using SKWReplay.Recording;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SKWReplay.Camera;

namespace SKWReplay.App;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private readonly SKWReplay.Camera.CameraCapture _camera = new();
    private readonly RecordingManager _recording = new();
    private readonly DelayRingBuffer _ring = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private readonly Stopwatch _duration = new();
    private DateTimeOffset _countdownEnd;
    private RecordingState _state = RecordingState.Ready;
    private int _delay;
    private int _countdown;
    private CameraDevice? _selectedCamera;
    private BitmapSource? _preview;
    private string _statusMessage = "Bereit";
    private string _pauseButtonText = "Ⅱ   Aufnahme pausieren";
    private string _recordingBadge = "";
    private Visibility _recordingBadgeVisibility = Visibility.Collapsed;
    private Visibility _countdownVisibility = Visibility.Collapsed;
    private double _zoom = 1.0;
    private bool _disposed;

    public ObservableCollection<CameraDevice> Cameras { get; } = new();

    public CameraDevice? SelectedCamera
    {
        get => _selectedCamera;
        set
        {
            if (SetProperty(ref _selectedCamera, value) && value is not null && _state is RecordingState.Ready)
                OpenCamera(value);
        }
    }

    public BitmapSource? Preview { get => _preview; private set => SetProperty(ref _preview, value); }
    public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }
    public string PauseButtonText { get => _pauseButtonText; private set => SetProperty(ref _pauseButtonText, value); }
    public string RecordingBadge { get => _recordingBadge; private set => SetProperty(ref _recordingBadge, value); }
    public Visibility RecordingBadgeVisibility { get => _recordingBadgeVisibility; private set => SetProperty(ref _recordingBadgeVisibility, value); }
    public Visibility CountdownVisibility { get => _countdownVisibility; private set => SetProperty(ref _countdownVisibility, value); }
    public string DelayText => $"{_delay} s";
    public string Countdown => _countdown.ToString();

    public RelayCommand DelayUpCommand { get; }
    public RelayCommand DelayDownCommand { get; }
    public RelayCommand RefreshCamerasCommand { get; }
    public RelayCommand StartCommand { get; }
    public RelayCommand PauseCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand FinishCommand { get; }

    public MainViewModel()
    {
        DelayUpCommand = new RelayCommand(_ => AdjustDelay(1), _ => _state is RecordingState.Ready);
        DelayDownCommand = new RelayCommand(_ => AdjustDelay(-1), _ => _state is RecordingState.Ready);
        RefreshCamerasCommand = new RelayCommand(_ => RefreshCameras());
        StartCommand = new RelayCommand(_ => StartRecording(), _ => _selectedCamera is not null && _state is RecordingState.Ready);
        PauseCommand = new RelayCommand(_ => TogglePause(), _ => _state is RecordingState.Recording or RecordingState.Paused);
        SaveCommand = new RelayCommand(_ => _ = SaveAsync(), _ => _state is RecordingState.Finished);
        FinishCommand = new RelayCommand(_ => FinishRecording(), _ => _state is RecordingState.Recording or RecordingState.Paused);

        _camera.FrameReceived += OnCameraFrame;
        _camera.ErrorOccurred += message => Application.Current.Dispatcher.Invoke(() =>
        {
            StatusMessage = message;
            _state = RecordingState.Error;
            RaiseStateCommands();
        });
        _timer.Tick += (_, _) => UpdateTimer();
        RefreshCameras();
        _timer.Start();
    }

    private void RefreshCameras()
    {
        Cameras.Clear();
        foreach (var camera in SKWReplay.Camera.CameraEnumerator.GetAvailableCameras())
            Cameras.Add(camera);
        if (Cameras.Count > 0)
            SelectedCamera = Cameras[0];
        else
            StatusMessage = "Keine Kamera gefunden";
    }

    private void OpenCamera(CameraDevice camera)
    {
        StatusMessage = "Kamera wird initialisiert";
        if (!_camera.Start(camera.Index))
            StatusMessage = "Kamera konnte nicht geöffnet werden";
        else
            StatusMessage = "Bereit";
    }

    private void AdjustDelay(int change)
    {
        _delay = Math.Clamp(_delay + change, 0, 60);
        Raise(nameof(DelayText));
    }

    public void AdjustZoom(double change)
    {
        _zoom = Math.Clamp(_zoom + change, 1.0, 4.0);
        _camera.Zoom = _zoom;
    }

    private void StartRecording()
    {
        _ring.Clear();
        _countdown = _delay;
        CountdownVisibility = _delay > 0 ? Visibility.Visible : Visibility.Collapsed;
        _state = RecordingState.Recording;
        StatusMessage = _delay > 0 ? "Verzögerungspuffer wird aufgebaut" : "Aufnahme läuft";
        RecordingBadge = "● REC";
        RecordingBadgeVisibility = Visibility.Visible;
        _duration.Restart();
        _countdownEnd = DateTimeOffset.UtcNow.AddSeconds(_delay);
        RaiseStateCommands();
    }

    private void TogglePause()
    {
        if (_state == RecordingState.Recording)
        {
            _recording.Pause();
            _duration.Stop();
            _ring.Clear();
            _state = RecordingState.Paused;
            StatusMessage = "Aufnahme pausiert";
            RecordingBadgeVisibility = Visibility.Collapsed;
            PauseButtonText = "▶   Aufnahme fortsetzen";
        }
        else if (_state == RecordingState.Paused)
        {
            _countdown = _delay;
            CountdownVisibility = _delay > 0 ? Visibility.Visible : Visibility.Collapsed;
            _state = RecordingState.Recording;
            StatusMessage = "Aufnahme läuft";
            RecordingBadge = "● REC";
            RecordingBadgeVisibility = Visibility.Visible;
            PauseButtonText = "Ⅱ   Aufnahme pausieren";
            _duration.Start();
            _countdownEnd = DateTimeOffset.UtcNow.AddSeconds(_delay);
        }
        RaiseStateCommands();
    }

    private void OnCameraFrame(CameraFrame frame)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_state == RecordingState.Recording)
            {
                if (!_recording.IsRecording)
                {
                    if (!_recording.StartSegment(frame.Image))
                    {
                        StatusMessage = _recording.LastError ?? "Encoding konnte nicht gestartet werden";
                        _state = RecordingState.Error;
                        RecordingBadgeVisibility = Visibility.Collapsed;
                        RaiseStateCommands();
                        frame.Image.Dispose();
                        return;
                    }
                }
                _recording.WriteFrame(frame.Image);
            }

            if (_state is RecordingState.Recording or RecordingState.Ready)
            {
                using var previewMat = new Mat();
                Cv2.CvtColor(frame.Image, previewMat, ColorConversionCodes.BGR2BGRA);
                int previewStride = checked((int)previewMat.Step());
                Preview = BitmapSource.Create(
                    previewMat.Width, previewMat.Height, 96, 96, PixelFormats.Bgra32, null,
                    previewMat.Data, previewStride * previewMat.Rows, previewStride);

                if (_state == RecordingState.Recording)
                {
                    byte[] jpeg = frame.Image.ToBytes(".jpg",
                        new ImageEncodingParam(ImwriteFlags.JpegQuality, 80));
                    _ring.Add(jpeg);
                    if (_countdown == 0)
                    {
                        var delayed = _ring.GetDelayedFrame(_delay);
                        if (delayed is not null)
                            Preview = LoadJpeg(delayed.Jpeg);
                    }
                }
            }
            frame.Image.Dispose();
        });
    }

    private BitmapSource LoadJpeg(byte[] jpeg)
    {
        using var stream = new MemoryStream(jpeg);
        var decoder = new JpegBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        frame.Freeze();
        return frame;
    }

    private void UpdateTimer()
    {
        if (_state == RecordingState.Recording)
        {
            if (_countdown > 0)
            {
                int remaining = Math.Max(0, (int)Math.Ceiling((_countdownEnd - DateTimeOffset.UtcNow).TotalSeconds));
                if (remaining != _countdown)
                {
                    _countdown = remaining;
                    Raise(nameof(Countdown));
                }
            }
            if (_countdown == 0)
                CountdownVisibility = Visibility.Collapsed;

            TimeSpan elapsed = _duration.Elapsed;
            RecordingBadge = $"● REC  {elapsed:hh\\:mm\\:ss}";
        }
    }

    private void FinishRecording()
    {
        _recording.Stop();
        _duration.Stop();
        _state = RecordingState.Finished;
        StatusMessage = "Aufnahme beendet";
        RecordingBadgeVisibility = Visibility.Collapsed;
        CountdownVisibility = Visibility.Collapsed;
        RaiseStateCommands();
    }

    private async Task SaveAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Aufnahme speichern",
            Filter = "MP4-Datei (*.mp4)|*.mp4",
            DefaultExt = ".mp4",
            FileName = $"SKWReplay-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.mp4"
        };
        if (dialog.ShowDialog() != true)
            return;

        if (!RecordingManager.HasFreeDiskSpace(dialog.FileName, 1024UL * 1024 * 1024))
        {
            MessageBox.Show("Nicht genügend Speicherplatz auf dem Ziellaufwerk.", "Speicherplatz", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _state = RecordingState.Processing;
        StatusMessage = "Aufnahme wird verarbeitet";
        RaiseStateCommands();
        var progress = new Progress<int>(value => StatusMessage = $"Speichern {value}%");
        bool success = await _recording.SaveAsync(dialog.FileName, progress);
        if (success)
        {
            _state = RecordingState.Saved;
            StatusMessage = "Aufnahme gespeichert";
            MessageBox.Show($"Das Video wurde gespeichert:\n{dialog.FileName}", "Erfolg",
                MessageBoxButton.OK, MessageBoxImage.Information);
            _recording.Cleanup();
        }
        else
        {
            _state = RecordingState.Error;
            StatusMessage = _recording.LastError ?? "Fehler beim Speichern";
        }
        RaiseStateCommands();
    }

    private void RaiseStateCommands()
    {
        StartCommand.RaiseCanExecuteChanged();
        PauseCommand.RaiseCanExecuteChanged();
        SaveCommand.RaiseCanExecuteChanged();
        FinishCommand.RaiseCanExecuteChanged();
        DelayUpCommand.RaiseCanExecuteChanged();
        DelayDownCommand.RaiseCanExecuteChanged();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _camera.Dispose();
        _recording.Dispose();
    }
}
