using System.Windows;
using System.Windows.Input;

namespace SKWReplay.App;

public partial class SaveSuccessWindow : Window
{
    public string SavedFilePath { get; }

    public SaveSuccessWindow(string savedFilePath)
    {
        SavedFilePath = savedFilePath;
        InitializeComponent();
        DataContext = this;
    }

    private void Done_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
    }
}
