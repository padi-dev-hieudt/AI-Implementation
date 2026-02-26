using RBAC_WPF_2026.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace RBAC_WPF_2026;

public class ViewModelTemplateSelector : DataTemplateSelector
{
    public DataTemplate? UserManagementTemplate { get; set; }
    public DataTemplate? RoleManagementTemplate { get; set; }
    public DataTemplate? PermissionManagementTemplate { get; set; }
    public DataTemplate? PositionManagementTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        return item switch
        {
            UserManagementViewModel => UserManagementTemplate,
            RoleManagementViewModel => RoleManagementTemplate,
            PermissionManagementViewModel => PermissionManagementTemplate,
            PositionManagementViewModel => PositionManagementTemplate,
            _ => base.SelectTemplate(item, container)
        };
    }
}