using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RBAC_WPF_2026.Commands;
using RBAC_WPF_2026.Data;
using RBAC_WPF_2026.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace RBAC_WPF_2026.ViewModels;

public class PermissionManagementViewModel : BaseViewModel
{
    private readonly AppDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private Permission? _selectedPermission;
    private string _message = string.Empty;
    private bool _isLoading;

    public ObservableCollection<Permission> Permissions { get; set; } = new();

    public Permission? SelectedPermission
    {
        get => _selectedPermission;
        set => SetProperty(ref _selectedPermission, value);
    }

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public ICommand AddPermissionCommand { get; }
    public ICommand EditPermissionCommand { get; }
    public ICommand DeletePermissionCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand EditPermissionContextCommand { get; }
    public ICommand DeletePermissionContextCommand { get; }

    public PermissionManagementViewModel(AppDbContext context, IServiceProvider serviceProvider)
    {
        _context = context;
        _serviceProvider = serviceProvider;

        AddPermissionCommand = new RelayCommand(AddPermission);
        EditPermissionCommand = new RelayCommand(EditPermission, () => SelectedPermission != null);
        DeletePermissionCommand = new RelayCommand(DeletePermission, () => SelectedPermission != null);
        RefreshCommand = new RelayCommand(LoadPermissions);
        EditPermissionContextCommand = new RelayCommand(param => EditPermissionWithParameter(param as Permission));
        DeletePermissionContextCommand = new RelayCommand(param => DeletePermissionWithParameter(param as Permission));

        LoadPermissions();
    }

    private async void LoadPermissions()
    {
        try
        {
            IsLoading = true;
            Message = "Loading permissions...";
            
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var permissions = await context.Permissions.ToListAsync();

            Permissions.Clear();
            foreach (var permission in permissions)
            {
                Permissions.Add(permission);
            }

            Message = $"Loaded {permissions.Count} permissions.";
        }
        catch (Exception ex)
        {
            Message = $"Error loading permissions: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void AddPermission()
    {
        var dialog = new PermissionEditViewModel(_serviceProvider);
        if (ShowPermissionDialog(dialog) == true)
        {
            LoadPermissions();
            Message = "Permission added successfully.";
        }
    }

    private void EditPermission()
    {
        if (SelectedPermission == null) return;

        var dialog = new PermissionEditViewModel(_serviceProvider, SelectedPermission);
        if (ShowPermissionDialog(dialog) == true)
        {
            LoadPermissions();
            Message = "Permission updated successfully.";
        }
    }

    private async void DeletePermission()
    {
        if (SelectedPermission == null) return;

        var result = MessageBox.Show(
            $"Are you sure you want to delete permission '{SelectedPermission.Name}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                var permissionToDelete = await context.Permissions.FindAsync(SelectedPermission.Id);
                if (permissionToDelete != null)
                {
                    context.Permissions.Remove(permissionToDelete);
                    await context.SaveChangesAsync();
                    LoadPermissions();
                    Message = "Permission deleted successfully.";
                }
            }
            catch (Exception ex)
            {
                Message = $"Error deleting permission: {ex.Message}";
            }
        }
    }

    private bool? ShowPermissionDialog(PermissionEditViewModel viewModel)
    {
        var dialog = new Views.PermissionEditDialog
        {
            DataContext = viewModel,
            Owner = Application.Current.MainWindow
        };

        return dialog.ShowDialog();
    }

    private void EditPermissionWithParameter(Permission? permission)
    {
        if (permission == null) return;

        var dialog = new PermissionEditViewModel(_serviceProvider, permission);
        if (ShowPermissionDialog(dialog) == true)
        {
            LoadPermissions();
            Message = "Permission updated successfully.";
        }
    }

    private async void DeletePermissionWithParameter(Permission? permission)
    {
        if (permission == null) return;

        var result = MessageBox.Show(
            $"Are you sure you want to delete permission '{permission.Name}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var permissionToDelete = await context.Permissions.FindAsync(permission.Id);
                if (permissionToDelete != null)
                {
                    context.Permissions.Remove(permissionToDelete);
                    await context.SaveChangesAsync();
                    LoadPermissions();
                    Message = "Permission deleted successfully.";
                }
            }
            catch (Exception ex)
            {
                Message = $"Error deleting permission: {ex.Message}";
            }
        }
    }
}