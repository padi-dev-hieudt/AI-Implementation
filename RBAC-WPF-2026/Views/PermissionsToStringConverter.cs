using RBAC_WPF_2026.Models;
using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace RBAC_WPF_2026.Views;

public class PermissionsToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IEnumerable<RolePermission> rolePermissions)
        {
            return string.Join(", ", rolePermissions.Select(rp => rp.Permission?.Name ?? "Unknown"));
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}