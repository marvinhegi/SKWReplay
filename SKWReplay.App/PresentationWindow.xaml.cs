using Forms = System.Windows.Forms;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace SKWReplay.App;

public partial class PresentationWindow : Window
{
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;
    private static readonly IntPtr HwndTopmost = new(-1);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr windowHandle, IntPtr insertAfter,
        int x, int y, int width, int height, uint flags);

    private readonly MainViewModel _viewModel;
    private readonly Forms.Screen _screen;

    public PresentationWindow(MainViewModel viewModel, Forms.Screen screen)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _screen = screen;
        DataContext = viewModel;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = screen.Bounds.Left;
        Top = screen.Bounds.Top;
        Width = screen.Bounds.Width;
        Height = screen.Bounds.Height;
        SizeChanged += (_, _) => UpdatePreviewSize(_viewModel.Preview);
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        Closed += (_, _) => _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        Loaded += (_, _) =>
        {
            var bounds = _screen.Bounds;
            IntPtr handle = new WindowInteropHelper(this).Handle;
            if (!SetWindowPos(handle, HwndTopmost, bounds.Left, bounds.Top,
                    bounds.Width, bounds.Height, SwpNoActivate | SwpShowWindow))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            UpdatePreviewSize(_viewModel.Preview);
        };
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.Preview))
            UpdatePreviewSize(_viewModel.Preview);
    }

    private void UpdatePreviewSize(BitmapSource? source)
    {
        if (source is null || ActualWidth <= 0 || ActualHeight <= 0)
        {
            PreviewImage.Width = 0;
            PreviewImage.Height = 0;
            return;
        }

        double sourceWidth = source.PixelWidth * 96.0 / source.DpiX;
        double sourceHeight = source.PixelHeight * 96.0 / source.DpiY;
        double scale = Math.Min(ActualWidth / sourceWidth, ActualHeight / sourceHeight);
        PreviewImage.Width = sourceWidth * scale;
        PreviewImage.Height = sourceHeight * scale;
    }
}
