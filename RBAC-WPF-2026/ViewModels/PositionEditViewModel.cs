using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RBAC_WPF_2026.Commands;
using RBAC_WPF_2026.Data;
using RBAC_WPF_2026.Models;
using System.Windows.Input;

namespace RBAC_WPF_2026.ViewModels;

public class PositionEditViewModel : BaseViewModel
{
    private readonly IServiceProvider _serviceProvider;
    private readonly int? _positionId;
    private string _name = string.Empty;
    private bool _isActive = true;
    private string _message = string.Empty;
    private bool _isLoading;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
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

    public bool IsEditMode => _positionId.HasValue;
    public string WindowTitle => IsEditMode ? "Edit Position" : "Add Position";
    public string SaveButtonText => IsEditMode ? "Update" : "Create";

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public PositionEditViewModel(IServiceProvider serviceProvider, int? positionId = null)
    {
        _serviceProvider = serviceProvider;
        _positionId = positionId;

        SaveCommand = new RelayCommand(Save, CanSave);
        CancelCommand = new RelayCommand(Cancel);

        if (IsEditMode)
        {
            LoadPosition();
        }
    }

    private async void LoadPosition()
    {
        if (!_positionId.HasValue)
            return;

        try
        {
            IsLoading = true;
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var position = await context.Positions.FindAsync(_positionId.Value);
            if (position != null)
            {
                Name = position.Name;
                IsActive = position.IsActive;
            }
            else
            {
                Message = "Position not found";
            }
        }
        catch (Exception ex)
        {
            Message = $"Error loading position: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(Name) && !IsLoading;
    }

    private async void Save()
    {
        if (!CanSave())
            return;

        try
        {
            IsLoading = true;
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (IsEditMode)
            {
                var position = await context.Positions.FindAsync(_positionId!.Value);
                if (position != null)
                {
                    position.Name = Name.Trim();
                    position.IsActive = IsActive;
                    position.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    Message = "Position not found";
                    return;
                }
            }
            else
            {
                var position = new Position
                {
                    Name = Name.Trim(),
                    IsActive = IsActive,
                    CreatedAt = DateTime.UtcNow
                };
                context.Positions.Add(position);
            }

            await context.SaveChangesAsync();
            
            // Close dialog with success result
            if (System.Windows.Application.Current.MainWindow?.OwnedWindows.Count > 0)
            {
                var dialog = System.Windows.Application.Current.MainWindow.OwnedWindows
                    .OfType<System.Windows.Window>().LastOrDefault();
                if (dialog != null)
                {
                    dialog.DialogResult = true;
                    dialog.Close();
                }
            }
        }
        catch (Exception ex)
        {
            Message = $"Error saving position: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Cancel()
    {
        // Close dialog with cancel result
        if (System.Windows.Application.Current.MainWindow?.OwnedWindows.Count > 0)
        {
            var dialog = System.Windows.Application.Current.MainWindow.OwnedWindows
                .OfType<System.Windows.Window>().LastOrDefault();
            if (dialog != null)
            {
                dialog.DialogResult = false;
                dialog.Close();
            }
        }
    }
}