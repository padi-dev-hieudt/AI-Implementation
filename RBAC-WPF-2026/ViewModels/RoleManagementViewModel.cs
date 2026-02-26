using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RBAC_WPF_2026.Commands;
using RBAC_WPF_2026.Data;
using RBAC_WPF_2026.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace RBAC_WPF_2026.ViewModels;

public class RoleManagementViewModel : BaseViewModel
{
    private readonly AppDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private Role? _selectedRole;
    private string _message = string.Empty;
    private bool _isLoading;

    public ObservableCollection<Role> Roles { get; set; } = new();

    public Role? SelectedRole
    {
        get => _selectedRole;
        set => SetProperty(ref _selectedRole, value);
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

    public ICommand AddRoleCommand { get; }
    public ICommand EditRoleCommand { get; }
    public ICommand DeleteRoleCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand EditRoleContextCommand { get; }
    public ICommand DeleteRoleContextCommand { get; }

    public RoleManagementViewModel(AppDbContext context, IServiceProvider serviceProvider)
    {
        _context = context;
        _serviceProvider = serviceProvider;

        AddRoleCommand = new RelayCommand(AddRole);
        EditRoleCommand = new RelayCommand(EditRole, () => SelectedRole != null);
        DeleteRoleCommand = new RelayCommand(DeleteRole, () => SelectedRole != null);
        RefreshCommand = new RelayCommand(LoadRoles);
        EditRoleContextCommand = new RelayCommand(param => EditRoleWithParameter(param as Role));
        DeleteRoleContextCommand = new RelayCommand(param => DeleteRoleWithParameter(param as Role));

        LoadRoles();
    }

    private async void LoadRoles()
    {
        try
        {
            IsLoading = true;
            Message = "Loading roles...";
            
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var roles = await context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .ToListAsync();

            Roles.Clear();
            foreach (var role in roles)
            {
                Roles.Add(role);
            }

            Message = $"Loaded {roles.Count} roles.";
        }
        catch (Exception ex)
        {
            Message = $"Error loading roles: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void AddRole()
    {
        var dialog = new RoleEditViewModel(_context, _serviceProvider);
        if (ShowRoleDialog(dialog) == true)
        {
            LoadRoles();
            Message = "Role added successfully.";
        }
    }

    private void EditRole()
    {
        if (SelectedRole == null) return;

        var dialog = new RoleEditViewModel(_context, _serviceProvider, SelectedRole);
        if (ShowRoleDialog(dialog) == true)
        {
            LoadRoles();
            Message = "Role updated successfully.";
        }
    }

    private async void DeleteRole()
    {
        if (SelectedRole == null) return;

        var result = MessageBox.Show(
            $"Are you sure you want to delete role '{SelectedRole.Name}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                var roleToDelete = await context.Roles.FindAsync(SelectedRole.Id);
                if (roleToDelete != null)
                {
                    context.Roles.Remove(roleToDelete);
                    await context.SaveChangesAsync();
                    LoadRoles();
                    Message = "Role deleted successfully.";
                }
            }
            catch (Exception ex)
            {
                Message = $"Error deleting role: {ex.Message}";
            }
        }
    }

    private bool? ShowRoleDialog(RoleEditViewModel viewModel)
    {
        var dialog = new Views.RoleEditDialog
        {
            DataContext = viewModel,
            Owner = Application.Current.MainWindow
        };

        return dialog.ShowDialog();
    }

    private void EditRoleWithParameter(Role? role)
    {
        if (role == null) return;

        var dialog = new RoleEditViewModel(_context, _serviceProvider, role);
        if (ShowRoleDialog(dialog) == true)
        {
            LoadRoles();
            Message = "Role updated successfully.";
        }
    }

    private async void DeleteRoleWithParameter(Role? role)
    {
        if (role == null) return;

        var result = MessageBox.Show(
            $"Are you sure you want to delete role '{role.Name}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var roleToDelete = await context.Roles.FindAsync(role.Id);
                if (roleToDelete != null)
                {
                    context.Roles.Remove(roleToDelete);
                    await context.SaveChangesAsync();
                    LoadRoles();
                    Message = "Role deleted successfully.";
                }
            }
            catch (Exception ex)
            {
                Message = $"Error deleting role: {ex.Message}";
            }
        }
    }
}