using System.Windows;
using System.Windows.Controls;
using RBAC_WPF_2026.ViewModels;
using RBAC_WPF_2026.Models;

namespace RBAC_WPF_2026.Views;

public partial class RoleManagementView : UserControl
{
    public RoleManagementView()
    {
        InitializeComponent();
        RolesDataGrid.LoadingRow += RolesDataGrid_LoadingRow;
    }

    private void RolesDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        var role = e.Row.DataContext as Role;
        if (role != null && DataContext is RoleManagementViewModel viewModel)
        {
            var contextMenu = new ContextMenu();
            
            var editMenuItem = new MenuItem()
            {
                Header = "✏️ Edit Role",
                Command = viewModel.EditRoleContextCommand,
                CommandParameter = role
            };
            
            var deleteMenuItem = new MenuItem()
            {
                Header = "🗑️ Delete Role",
                Command = viewModel.DeleteRoleContextCommand,
                CommandParameter = role,
                Foreground = System.Windows.Media.Brushes.Red
            };
            
            contextMenu.Items.Add(editMenuItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(deleteMenuItem);
            
            e.Row.ContextMenu = contextMenu;
        }
    }
}