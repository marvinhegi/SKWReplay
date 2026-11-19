using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Forms = System.Windows.Forms;

namespace SKWReplay.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private readonly List<PresentationWindow> _presentationWindows = new();
    private WindowStyle _windowedStyle;
    private WindowState _windowedState;
    private ResizeMode _windowedResizeMode;
    private double _windowedLeft;
    private double _windowedTop;
    private double _windowedWidth;
    private double _windowedHeight;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        Closed += (_, _) =>
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            ClosePresentationWindows();
            _viewModel.Dispose();
        };
    }

    private void Preview_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        _viewModel.AdjustZoom(e.Delta > 0 ? 0.15 : -0.15);
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsFullscreen))
            SetFullscreen(_viewModel.IsFullscreen);
    }

    private void SetFullscreen(bool fullscreen)
    {
        if (fullscreen)
            EnterFullscreen();
        else
            ExitFullscreen();
    }

    private void EnterFullscreen()
    {
        _windowedStyle = WindowStyle;
        _windowedState = WindowState;
        _windowedResizeMode = ResizeMode;
        _windowedLeft = Left;
        _windowedTop = Top;
        _windowedWidth = Width;
        _windowedHeight = Height;

        Forms.Screen? primary = Forms.Screen.PrimaryScreen;
        if (primary is not null)
        {
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Left = primary.Bounds.Left;
            Top = primary.Bounds.Top;
            WindowState = WindowState.Maximized;
        }

        foreach (Forms.Screen screen in Forms.Screen.AllScreens.Where(screen => !screen.Primary))
        {
            var preview = new PresentationWindow(_viewModel, screen);
            _presentationWindows.Add(preview);
            preview.Show();
        }
    }

    private void ExitFullscreen()
    {
        ClosePresentationWindows();
        WindowState = WindowState.Normal;
        WindowStyle = _windowedStyle;
        ResizeMode = _windowedResizeMode;
        Left = _windowedLeft;
        Top = _windowedTop;
        Width = _windowedWidth;
        Height = _windowedHeight;
        WindowState = _windowedState;
    }

    private void ClosePresentationWindows()
    {
        foreach (PresentationWindow window in _presentationWindows.ToArray())
            window.Close();
        _presentationWindows.Clear();
    }
}
