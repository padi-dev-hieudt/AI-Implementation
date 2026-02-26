using System.Windows;
using System.Windows.Controls;
using RBAC_WPF_2026.ViewModels;
using RBAC_WPF_2026.Models;

namespace RBAC_WPF_2026.Views;

public partial class PermissionManagementView : UserControl
{
    public PermissionManagementView()
    {
        InitializeComponent();
        PermissionsDataGrid.LoadingRow += PermissionsDataGrid_LoadingRow;
    }

    private void PermissionsDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        var permission = e.Row.DataContext as Permission;
        if (permission != null && DataContext is PermissionManagementViewModel viewModel)
        {
            var contextMenu = new ContextMenu();
            
            var editMenuItem = new MenuItem()
            {
                Header = "✏️ Edit Permission",
                Command = viewModel.EditPermissionContextCommand,
                CommandParameter = permission
            };
            
            var deleteMenuItem = new MenuItem()
            {
                Header = "🗑️ Delete Permission",
                Command = viewModel.DeletePermissionContextCommand,
                CommandParameter = permission,
                Foreground = System.Windows.Media.Brushes.Red
            };
            
            contextMenu.Items.Add(editMenuItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(deleteMenuItem);
            
            e.Row.ContextMenu = contextMenu;
        }
    }
}