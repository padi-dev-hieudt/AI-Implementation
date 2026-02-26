using RBAC_WPF_2026.Models;
using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace RBAC_WPF_2026.Views;

public class RolesToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IEnumerable<UserRole> userRoles)
        {
            return string.Join(", ", userRoles.Select(ur => ur.Role?.Name ?? "Unknown"));
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}