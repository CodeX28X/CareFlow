
using Avalonia.Controls;
using CareFlow.Desktop.ViewModels;

namespace CareFlow.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }
}
