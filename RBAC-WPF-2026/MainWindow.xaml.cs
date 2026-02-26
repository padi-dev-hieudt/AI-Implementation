using RBAC_WPF_2026.Data;
using RBAC_WPF_2026.ViewModels;
using System.Windows;

namespace RBAC_WPF_2026;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel(serviceProvider);
    }
}