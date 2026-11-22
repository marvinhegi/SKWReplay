using Microsoft.Win32;
using OpenCvSharp;
using SKWReplay.Core;
using SKWReplay.Recording;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SKWReplay.Camera;
using WpfApplication = System.Windows.Application;
using WpfMessageBox = System.Windows.MessageBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace SKWReplay.App;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private static readonly string DelaySettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SKWReplay",
        "delay-seconds.txt");

    private readonly SKWReplay.Camera.CameraCapture _camera = new();
    private readonly RecordingManager _recording = new();
    private readonly DelayRingBuffer _ring = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private readonly Stopwatch _duration = new();
    private DateTimeOffset _countdownEnd;
    private DateTimeOffset _bufferStartedAt;
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
    private bool _isSettingsExpanded = true;
    private bool _isFullscreen;
    private bool _disposed;

    public ObservableCollection<CameraDevice> Cameras { get; } = new();

    public CameraDevice? SelectedCamera
    {
        get => _selectedCamera;
        set
        {
            if (SetProperty(ref _selectedCamera, value) && value is not null && _state is RecordingState.Ready)
                OpenCamera(value);
            RaiseStateCommands();
        }
    }

    public BitmapSource? Preview { get => _preview; private set => SetProperty(ref _preview, value); }
    public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }
    public string PauseButtonText { get => _pauseButtonText; private set => SetProperty(ref _pauseButtonText, value); }
    public string RecordingBadge { get => _recordingBadge; private set => SetProperty(ref _recordingBadge, value); }
    public Visibility RecordingBadgeVisibility { get => _recordingBadgeVisibility; private set => SetProperty(ref _recordingBadgeVisibility, value); }
    public Visibility CountdownVisibility { get => _countdownVisibility; private set => SetProperty(ref _countdownVisibility, value); }
    public Visibility StartVisibility => _state == RecordingState.Ready && _selectedCamera is not null ? Visibility.Visible : Visibility.Collapsed;
    public Visibility PauseVisibility => _state is RecordingState.Recording or RecordingState.Paused ? Visibility.Visible : Visibility.Collapsed;
    public Visibility FinishVisibility => PauseVisibility;
    public Visibility SaveVisibility => _state == RecordingState.Finished ? Visibility.Visible : Visibility.Collapsed;
    public Visibility DiscardVisibility => SaveVisibility;
    public Visibility SettingsPanelVisibility => _isSettingsExpanded ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SettingsToggleVisibility => _isSettingsExpanded ? Visibility.Collapsed : Visibility.Visible;
    public Thickness PreviewMargin => _isSettingsExpanded ? new Thickness(0, 0, 16, 0) : new Thickness(0);
    public string FullscreenButtonText => _isFullscreen ? "Vollbild verlassen" : "Vollbild";
    public Geometry FullscreenIconGeometry => Geometry.Parse(_isFullscreen
        ? "M9,3 V9 H3 M15,3 V9 H21 M21,15 H15 V21 M9,21 V15 H3"
        : "M3,9 V3 H9 M15,3 H21 V9 M21,15 V21 H15 M9,21 H3 V15");
    public bool IsSettingsExpanded
    {
        get => _isSettingsExpanded;
        private set
        {
            if (SetProperty(ref _isSettingsExpanded, value))
            {
                Raise(nameof(SettingsPanelVisibility));
                Raise(nameof(SettingsToggleVisibility));
                Raise(nameof(PreviewMargin));
            }
        }
    }
    public bool IsFullscreen
    {
        get => _isFullscreen;
        private set
        {
            if (SetProperty(ref _isFullscreen, value))
            {
                Raise(nameof(FullscreenButtonText));
                Raise(nameof(FullscreenIconGeometry));
            }
        }
    }
    public string DelayText => $"{_delay} s";
    public string Countdown => _countdown.ToString();

    public RelayCommand DelayUpCommand { get; }
    public RelayCommand DelayDownCommand { get; }
    public RelayCommand RefreshCamerasCommand { get; }
    public RelayCommand StartCommand { get; }
    public RelayCommand PauseCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand FinishCommand { get; }
    public RelayCommand DiscardCommand { get; }
    public RelayCommand ToggleSettingsCommand { get; }
    public RelayCommand ToggleFullscreenCommand { get; }

    public MainViewModel()
    {
        DelayUpCommand = new RelayCommand(_ => AdjustDelay(1), _ => _state is RecordingState.Ready or RecordingState.Recording or RecordingState.Paused);
        DelayDownCommand = new RelayCommand(_ => AdjustDelay(-1), _ => _state is RecordingState.Ready or RecordingState.Recording or RecordingState.Paused);
        RefreshCamerasCommand = new RelayCommand(_ => RefreshCameras());
        StartCommand = new RelayCommand(_ => StartRecording(), _ => _selectedCamera is not null && _state is RecordingState.Ready);
        PauseCommand = new RelayCommand(_ => TogglePause(), _ => _state is RecordingState.Recording or RecordingState.Paused);
        SaveCommand = new RelayCommand(_ => _ = SaveAsync(), _ => _state is RecordingState.Finished);
        FinishCommand = new RelayCommand(_ => FinishRecording(), _ => _state is RecordingState.Recording or RecordingState.Paused);
        DiscardCommand = new RelayCommand(_ => DiscardRecording(), _ => _state is RecordingState.Finished);
        ToggleSettingsCommand = new RelayCommand(_ => IsSettingsExpanded = !IsSettingsExpanded);
        ToggleFullscreenCommand = new RelayCommand(_ => IsFullscreen = !IsFullscreen);

        _camera.FrameReceived += OnCameraFrame;
        _camera.ErrorOccurred += message => WpfApplication.Current.Dispatcher.Invoke(() =>
        {
            StatusMessage = message;
            _state = RecordingState.Error;
            RaiseStateCommands();
        });
        _timer.Tick += (_, _) => UpdateTimer();
        RefreshCameras();
        LoadDelaySetting();
        _timer.Start();
    }

    private void RefreshCameras()
    {
        try
        {
            Cameras.Clear();
            foreach (var camera in SKWReplay.Camera.CameraEnumerator.GetAvailableCameras())
                Cameras.Add(camera);

            if (Cameras.Count > 0)
                SelectedCamera = Cameras[0];
            else
            {
                SelectedCamera = null;
                StatusMessage = "Keine Kamera gefunden";
            }
            RaiseStateCommands();
        }
        catch (Exception ex)
        {
            Cameras.Clear();
            SelectedCamera = null;
            StatusMessage = $"Kameras konnten nicht erkannt werden: {ex.Message}";
            RaiseStateCommands();
        }
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
        if (_state == RecordingState.Recording)
        {
            _countdownEnd = _bufferStartedAt.AddSeconds(_delay);
            _countdown = Math.Max(0, (int)Math.Ceiling((_countdownEnd - DateTimeOffset.UtcNow).TotalSeconds));
            CountdownVisibility = _countdown > 0 ? Visibility.Visible : Visibility.Collapsed;
            if (_countdown > 0)
                Preview = null;
            StatusMessage = _countdown > 0 ? "Verzögerungspuffer wird aufgebaut" : "Verzögerte Vorschau aktiv";
            Raise(nameof(Countdown));
        }
        SaveDelaySetting();
    }

    private void LoadDelaySetting()
    {
        if (!File.Exists(DelaySettingsPath))
            return;

        try
        {
            string value = File.ReadAllText(DelaySettingsPath);
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int delay))
            {
                StatusMessage = "Die gespeicherte Verzögerungseinstellung ist ungültig.";
                return;
            }

            _delay = Math.Clamp(delay, 0, 60);
        }
        catch (IOException ex)
        {
            StatusMessage = $"Verzögerungseinstellung konnte nicht geladen werden: {ex.Message}";
        }
        catch (UnauthorizedAccessException ex)
        {
            StatusMessage = $"Verzögerungseinstellung konnte nicht geladen werden: {ex.Message}";
        }
    }

    private void SaveDelaySetting()
    {
        try
        {
            string directory = Path.GetDirectoryName(DelaySettingsPath)!;
            Directory.CreateDirectory(directory);
            string temporaryPath = $"{DelaySettingsPath}.tmp";
            File.WriteAllText(temporaryPath, _delay.ToString(CultureInfo.InvariantCulture));
            File.Move(temporaryPath, DelaySettingsPath, true);
        }
        catch (IOException ex)
        {
            StatusMessage = $"Verzögerungseinstellung konnte nicht gespeichert werden: {ex.Message}";
        }
        catch (UnauthorizedAccessException ex)
        {
            StatusMessage = $"Verzögerungseinstellung konnte nicht gespeichert werden: {ex.Message}";
        }
    }

    public void AdjustZoom(double change)
    {
        _zoom = Math.Clamp(_zoom + change, 1.0, 4.0);
        _camera.Zoom = _zoom;
    }

    private void StartRecording()
    {
        _ring.Clear();
        PauseButtonText = "Ⅱ   Aufnahme pausieren";
        _bufferStartedAt = DateTimeOffset.UtcNow;
        _countdown = _delay;
        Raise(nameof(Countdown));
        CountdownVisibility = _delay > 0 ? Visibility.Visible : Visibility.Collapsed;
        if (_delay > 0)
            Preview = null;
        _state = RecordingState.Recording;
        StatusMessage = _delay > 0 ? "Verzögerungspuffer wird aufgebaut" : "Aufnahme läuft";
        RecordingBadge = "● REC";
        RecordingBadgeVisibility = Visibility.Visible;
        _duration.Restart();
        _countdownEnd = _bufferStartedAt.AddSeconds(_delay);
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
            _bufferStartedAt = DateTimeOffset.UtcNow;
            _countdown = _delay;
            Raise(nameof(Countdown));
            CountdownVisibility = _delay > 0 ? Visibility.Visible : Visibility.Collapsed;
            if (_delay > 0)
                Preview = null;
            _state = RecordingState.Recording;
            StatusMessage = "Aufnahme läuft";
            RecordingBadge = "● REC";
            RecordingBadgeVisibility = Visibility.Visible;
            PauseButtonText = "Ⅱ   Aufnahme pausieren";
            _duration.Start();
            _countdownEnd = _bufferStartedAt.AddSeconds(_delay);
        }
        RaiseStateCommands();
    }

    private void OnCameraFrame(CameraFrame frame)
    {
        try
        {
            WpfApplication.Current.Dispatcher.Invoke(() =>
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
                            return;
                        }
                    }
                    _recording.WriteFrame(frame.Image);

                    Mat sourceImage = frame.SourceImage ?? frame.Image;
                    byte[] jpeg = sourceImage.ToBytes(".jpg",
                        new ImageEncodingParam(ImwriteFlags.JpegQuality, 80));
                    _ring.Add(jpeg, frame.Timestamp);
                    var delayed = _ring.GetDelayedFrame(_delay);
                    if (delayed is null)
                    {
                        Preview = CreatePreview(frame.Image);
                    }
                    else
                    {
                        using var delayedSource = Cv2.ImDecode(delayed.Jpeg, ImreadModes.Color);
                        using var zoomedDelayed = CameraCapture.ApplyZoom(delayedSource, _zoom);
                        Preview = CreatePreview(zoomedDelayed);
                    }
                }
                else if (_state == RecordingState.Ready)
                {
                    Preview = CreatePreview(frame.Image);
                }
            });
        }
        finally
        {
            frame.Image.Dispose();
            frame.SourceImage?.Dispose();
        }
    }

    private static BitmapSource CreatePreview(Mat image)
    {
        using var previewMat = new Mat();
        Cv2.CvtColor(image, previewMat, ColorConversionCodes.BGR2BGRA);
        int previewStride = checked((int)previewMat.Step());
        var bitmap = BitmapSource.Create(
            previewMat.Width, previewMat.Height, 96, 96, PixelFormats.Bgra32, null,
            previewMat.Data, previewStride * previewMat.Rows, previewStride);
        bitmap.Freeze();
        return bitmap;
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
                    if (remaining == 0)
                        StatusMessage = "Verzögerte Vorschau aktiv";
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

    private void DiscardRecording()
    {
        _recording.Cleanup();
        _ring.Clear();
        _duration.Reset();
        _countdown = 0;
        PauseButtonText = "Ⅱ   Aufnahme pausieren";
        _state = RecordingState.Ready;
        StatusMessage = "Aufnahme verworfen – bereit für neue Aufnahme";
        Preview = null;
        CountdownVisibility = Visibility.Collapsed;
        RecordingBadgeVisibility = Visibility.Collapsed;
        Raise(nameof(Countdown));
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
            WpfMessageBox.Show("Nicht genügend Speicherplatz auf dem Ziellaufwerk.", "Speicherplatz", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _state = RecordingState.Processing;
        StatusMessage = "Aufnahme wird verarbeitet";
        RaiseStateCommands();
        var progress = new Progress<int>(value => StatusMessage = $"Speichern {value}%");
        bool success = await _recording.SaveAsync(dialog.FileName, progress);
        if (success)
        {
            _recording.Cleanup();
            _ring.Clear();
            _duration.Reset();
            _state = RecordingState.Ready;
            StatusMessage = "Aufnahme gespeichert";
            var successWindow = new SaveSuccessWindow(dialog.FileName)
            {
                Owner = WpfApplication.Current.MainWindow
            };
            successWindow.ShowDialog();
        }
        else
        {
            _state = RecordingState.Finished;
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
        DiscardCommand.RaiseCanExecuteChanged();
        DelayUpCommand.RaiseCanExecuteChanged();
        DelayDownCommand.RaiseCanExecuteChanged();
        Raise(nameof(StartVisibility));
        Raise(nameof(PauseVisibility));
        Raise(nameof(FinishVisibility));
        Raise(nameof(SaveVisibility));
        Raise(nameof(DiscardVisibility));
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
