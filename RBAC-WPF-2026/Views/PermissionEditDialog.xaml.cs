using RBAC_WPF_2026.ViewModels;
using System.Windows;

namespace RBAC_WPF_2026.Views;

public partial class PermissionEditDialog : Window
{
    public PermissionEditDialog()
    {
        InitializeComponent();
        Loaded += PermissionEditDialog_Loaded;
    }

    private void PermissionEditDialog_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is PermissionEditViewModel viewModel)
        {
            viewModel.CloseAction = () => DialogResult = true;
        }
    }
}