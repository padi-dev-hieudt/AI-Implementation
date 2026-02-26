using RBAC_WPF_2026.ViewModels;
using System.Windows;

namespace RBAC_WPF_2026.Views;

public partial class UserEditDialog : Window
{
    public UserEditDialog()
    {
        InitializeComponent();
        Loaded += UserEditDialog_Loaded;
    }

    private void UserEditDialog_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserEditViewModel viewModel)
        {
            viewModel.CloseAction = () => DialogResult = true;
        }
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserEditViewModel viewModel)
        {
            viewModel.Password = PasswordBox.Password;
        }
    }
}