using System.Windows.Controls;
using System.Windows;
using RBAC_WPF_2026.ViewModels;
using RBAC_WPF_2026.Models;

namespace RBAC_WPF_2026.Views;

public partial class UserManagementView : UserControl
{
    public UserManagementView()
    {
        InitializeComponent();
        Loaded += UserManagementView_Loaded;
    }

    private void UserManagementView_Loaded(object sender, RoutedEventArgs e)
    {
        // Find the DataGrid and set up context menu programmatically
        var dataGrid = FindChild<DataGrid>(this, "UsersDataGrid");
        if (dataGrid != null)
        {
            dataGrid.LoadingRow += (s, args) =>
            {
                var row = args.Row;
                if (row.ContextMenu == null)
                {
                    var contextMenu = new ContextMenu();
                    
                    var editMenuItem = new MenuItem { Header = "✏️ Edit User" };
                    editMenuItem.Click += (sender, e) =>
                    {
                        if (DataContext is UserManagementViewModel vm && row.DataContext is User user)
                        {
                            vm.EditUserContextCommand.Execute(user);
                        }
                    };
                    
                    var deleteMenuItem = new MenuItem { Header = "🗑️ Delete User", Foreground = System.Windows.Media.Brushes.Red };
                    deleteMenuItem.Click += (sender, e) =>
                    {
                        if (DataContext is UserManagementViewModel vm && row.DataContext is User user)
                        {
                            vm.DeleteUserContextCommand.Execute(user);
                        }
                    };
                    
                    contextMenu.Items.Add(editMenuItem);
                    contextMenu.Items.Add(new Separator());
                    contextMenu.Items.Add(deleteMenuItem);
                    
                    row.ContextMenu = contextMenu;
                }
            };
        }
    }

    private static T? FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
    {
        if (parent == null) return null;

        T? foundChild = null;
        int childrenCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        
        for (int i = 0; i < childrenCount; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            
            if (child is T && (string.IsNullOrEmpty(childName) || (child as FrameworkElement)?.Name == childName))
            {
                foundChild = (T)child;
                break;
            }
            
            foundChild = FindChild<T>(child, childName);
            if (foundChild != null) break;
        }
        
        return foundChild;
    }
}