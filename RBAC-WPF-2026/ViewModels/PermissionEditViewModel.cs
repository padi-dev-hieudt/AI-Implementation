using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RBAC_WPF_2026.Commands;
using RBAC_WPF_2026.Data;
using RBAC_WPF_2026.Models;
using RBAC_WPF_2026.Services;
using System.Windows.Input;

namespace RBAC_WPF_2026.ViewModels;

public class PermissionEditViewModel : BaseViewModel
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Permission? _originalPermission;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _errorMessage = string.Empty;

    public bool IsEditMode => _originalPermission != null;
    public string Title => IsEditMode ? "Edit Permission" : "Add Permission";

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

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public Action? CloseAction { get; set; }

    public PermissionEditViewModel(IServiceProvider serviceProvider, Permission? permission = null)
    {
        _serviceProvider = serviceProvider;
        _originalPermission = permission;

        SaveCommand = new RelayCommand(Save, CanSave);
        CancelCommand = new RelayCommand(Cancel);

        if (permission != null)
        {
            Name = permission.Name;
            Description = permission.Description ?? string.Empty;
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
            ErrorMessage = string.Empty;

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (IsEditMode && _originalPermission != null)
            {
                var permission = await context.Permissions.FindAsync(_originalPermission.Id);
                if (permission != null)
                {
                    permission.Name = Name;
                    permission.Description = string.IsNullOrWhiteSpace(Description) ? null : Description;
                    context.Update(permission);
                }
                await context.SaveChangesAsync();
                NotificationService.Instance.ShowSuccess("Permission updated successfully!");
            }
            else
            {
                var permission = new Permission
                {
                    Name = Name,
                    Description = string.IsNullOrWhiteSpace(Description) ? null : Description
                };

                context.Permissions.Add(permission);
                await context.SaveChangesAsync();
                NotificationService.Instance.ShowSuccess("Permission created successfully!");
            }

            CloseAction?.Invoke();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true || ex.InnerException?.Message.Contains("duplicate") == true)
        {
            ErrorMessage = "Permission name already exists.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error saving permission: {ex.Message}";
        }
    }

    private void Cancel()
    {
        CloseAction?.Invoke();
    }
}