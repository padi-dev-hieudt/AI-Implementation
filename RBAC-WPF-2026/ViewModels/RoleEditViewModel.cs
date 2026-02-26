using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RBAC_WPF_2026.Commands;
using RBAC_WPF_2026.Data;
using RBAC_WPF_2026.Models;
using RBAC_WPF_2026.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace RBAC_WPF_2026.ViewModels;

public class RoleEditViewModel : BaseViewModel
{
    private readonly AppDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly Role? _originalRole;
    private int _id;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _message = string.Empty;
    private string _errorMessage = string.Empty;

    public bool IsEditMode => _originalRole != null;
    public string Title => IsEditMode ? "Edit Role" : "Add Role";

    public int Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public ObservableCollection<PermissionSelectionItem> AvailablePermissions { get; } = new();

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public Action? CloseAction { get; set; }
    public event Action? RoleSaved;

    public RoleEditViewModel(AppDbContext context, IServiceProvider serviceProvider, Role? role = null)
    {
        _context = context;
        _serviceProvider = serviceProvider;
        _originalRole = role;

        SaveCommand = new RelayCommand(Save, CanSave);
        CancelCommand = new RelayCommand(Cancel);

        if (role != null)
        {
            Id = role.Id;
            Name = role.Name;
            Description = role.Description ?? string.Empty;
        }

        LoadPermissions();
    }

    private async void LoadPermissions()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var permissions = await context.Permissions.ToListAsync();
            var rolePermissionIds = new HashSet<int>();
            
            // If editing an existing role, get its permissions
            if (_originalRole != null)
            {
                var roleWithPermissions = await context.Roles
                    .Include(r => r.RolePermissions)
                    .FirstOrDefaultAsync(r => r.Id == _originalRole.Id);
                rolePermissionIds = roleWithPermissions?.RolePermissions
                    .Select(rp => rp.PermissionId)
                    .ToHashSet() ?? new HashSet<int>();
            }

            AvailablePermissions.Clear();
            foreach (var permission in permissions)
            {
                AvailablePermissions.Add(new PermissionSelectionItem
                {
                    Permission = permission,
                    IsSelected = rolePermissionIds.Contains(permission.Id)
                });
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading permissions: {ex.Message}";
        }
    }

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(Name);
    }

    private async void Save()
    {
        try
        {
            Message = string.Empty;
            ErrorMessage = string.Empty;

            // Validate input
            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Role name is required.";
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (IsEditMode)
            {
                // Update existing role
                var role = await context.Roles.FindAsync(Id);
                if (role != null)
                {
                    role.Name = Name;
                    role.Description = Description ?? string.Empty;

                    await context.SaveChangesAsync();
                    
                    // Update role permissions
                    await UpdateRolePermissions(context, role);
                    
                    Message = "Role updated successfully!";
                    NotificationService.Instance.ShowSuccess("Role updated successfully!");
                    OnRoleSaved();
                }
            }
            else
            {
                // Check if role name already exists
                if (await context.Roles.AnyAsync(r => r.Name == Name))
                {
                    ErrorMessage = "Role name already exists.";
                    return;
                }

                // Create new role
                var role = new Role
                {
                    Name = Name,
                    Description = Description ?? string.Empty
                };

                context.Roles.Add(role);
                await context.SaveChangesAsync();
                
                // Update role permissions
                await UpdateRolePermissions(context, role);
                
                Message = "Role created successfully!";
                NotificationService.Instance.ShowSuccess("Role created successfully!");
                OnRoleSaved();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error saving role: {ex.Message}";
        }
    }

    private void OnRoleSaved()
    {
        RoleSaved?.Invoke();
        CloseAction?.Invoke();
    }

    private async Task UpdateRolePermissions(AppDbContext context, Role role)
    {
        // Remove existing permissions
        var existingRolePermissions = context.RolePermissions.Where(rp => rp.RoleId == role.Id);
        context.RolePermissions.RemoveRange(existingRolePermissions);

        // Add selected permissions
        var selectedPermissionIds = AvailablePermissions.Where(p => p.IsSelected).Select(p => p.Permission.Id);
        foreach (var permissionId in selectedPermissionIds)
        {
            context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionId });
        }

        await context.SaveChangesAsync();
    }

    private void Cancel()
    {
        CloseAction?.Invoke();
    }
}

public class PermissionSelectionItem : BaseViewModel
{
    private bool _isSelected;

    public Permission Permission { get; set; } = null!;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}