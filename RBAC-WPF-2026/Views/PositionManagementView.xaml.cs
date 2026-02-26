using System.Windows;
using System.Windows.Controls;
using RBAC_WPF_2026.ViewModels;
using RBAC_WPF_2026.Models;

namespace RBAC_WPF_2026.Views;

public partial class PositionManagementView : UserControl
{
    public PositionManagementView()
    {
        InitializeComponent();
        PositionsDataGrid.LoadingRow += PositionsDataGrid_LoadingRow;
    }

    private void PositionsDataGrid_LoadingRow(object? sender, DataGridRowEventArgs e)
    {
        var position = e.Row.DataContext as Position;
        if (position != null && DataContext is PositionManagementViewModel viewModel)
        {
            var contextMenu = new ContextMenu();
            
            var editMenuItem = new MenuItem()
            {
                Header = "✏️ Edit Position",
                Command = viewModel.EditPositionContextCommand,
                CommandParameter = position
            };
            
            var deleteMenuItem = new MenuItem()
            {
                Header = "🗑️ Delete Position",
                Command = viewModel.DeletePositionContextCommand,
                CommandParameter = position,
                Foreground = System.Windows.Media.Brushes.Red
            };
            
            contextMenu.Items.Add(editMenuItem);
            contextMenu.Items.Add(new Separator());
            contextMenu.Items.Add(deleteMenuItem);
            
            e.Row.ContextMenu = contextMenu;
        }
    }
}