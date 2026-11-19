using System.Windows;
using System.Windows.Input;

namespace SKWReplay.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        Closed += (_, _) => _viewModel.Dispose();
    }

    private void Preview_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        _viewModel.AdjustZoom(e.Delta > 0 ? 0.15 : -0.15);
    }
}
