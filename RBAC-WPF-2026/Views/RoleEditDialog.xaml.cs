using RBAC_WPF_2026.ViewModels;
using System.Windows;

namespace RBAC_WPF_2026.Views;

public partial class RoleEditDialog : Window
{
    public RoleEditDialog()
    {
        InitializeComponent();
        Loaded += RoleEditDialog_Loaded;
    }

    private void RoleEditDialog_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is RoleEditViewModel viewModel)
        {
            viewModel.CloseAction = () => DialogResult = true;
        }
    }
}