using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RBAC_WPF_2026.Commands;
using RBAC_WPF_2026.Data;
using RBAC_WPF_2026.Models;
using RBAC_WPF_2026.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace RBAC_WPF_2026.ViewModels;

public class PositionManagementViewModel : BaseViewModel
{
    private readonly AppDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly NotificationService _notificationService;
    private Position? _selectedPosition;
    private string _message = string.Empty;
    private bool _isLoading;

    public ObservableCollection<Position> Positions { get; set; } = new();

    public Position? SelectedPosition
    {
        get => _selectedPosition;
        set => SetProperty(ref _selectedPosition, value);
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

    public ICommand AddPositionCommand { get; }
    public ICommand EditPositionCommand { get; }
    public ICommand DeletePositionCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand EditPositionContextCommand { get; }
    public ICommand DeletePositionContextCommand { get; }
    public ICommand ToggleActiveCommand { get; }

    public PositionManagementViewModel(AppDbContext context, IServiceProvider serviceProvider, NotificationService notificationService)
    {
        _context = context;
        _serviceProvider = serviceProvider;
        _notificationService = notificationService;

        AddPositionCommand = new RelayCommand(AddPosition);
        EditPositionCommand = new RelayCommand(EditPosition, () => SelectedPosition != null);
        DeletePositionCommand = new RelayCommand(DeletePosition, () => SelectedPosition != null);
        RefreshCommand = new RelayCommand(LoadPositions);
        EditPositionContextCommand = new RelayCommand(param => EditPositionWithParameter(param as Position));
        DeletePositionContextCommand = new RelayCommand(param => DeletePositionWithParameter(param as Position));
        ToggleActiveCommand = new RelayCommand(param => ToggleActiveStatus(param as Position));

        LoadPositions();
    }

    private async void LoadPositions()
    {
        try
        {
            IsLoading = true;
            Message = "Loading positions...";
            
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var positions = await context.Positions.OrderBy(p => p.Name).ToListAsync();
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                Positions.Clear();
                foreach (var position in positions)
                {
                    Positions.Add(position);
                }
            });

            Message = $"Loaded {positions.Count} positions";
        }
        catch (Exception ex)
        {
            Message = $"Error loading positions: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void AddPosition()
    {
        try
        {
            var dialog = new Views.PositionEditDialog();
            var viewModel = new PositionEditViewModel(_serviceProvider);
            dialog.DataContext = viewModel;

            if (dialog.ShowDialog() == true)
            {
                LoadPositions();
                _notificationService.ShowSuccess("Position created successfully!");
            }
        }
        catch (Exception ex)
        {
            Message = $"Error adding position: {ex.Message}";
            _notificationService.ShowError($"Failed to create position: {ex.Message}");
        }
    }

    private void EditPosition()
    {
        if (SelectedPosition != null)
        {
            EditPositionWithParameter(SelectedPosition);
        }
    }

    private void EditPositionWithParameter(Position? position)
    {
        if (position != null)
        {
            try
            {
                var dialog = new Views.PositionEditDialog();
                var viewModel = new PositionEditViewModel(_serviceProvider, position.Id);
                dialog.DataContext = viewModel;

                if (dialog.ShowDialog() == true)
                {
                    LoadPositions();
                    _notificationService.ShowSuccess("Position updated successfully!");
                }
            }
            catch (Exception ex)
            {
                Message = $"Error editing position: {ex.Message}";
                _notificationService.ShowError($"Failed to update position: {ex.Message}");
            }
        }
    }

    private async void DeletePosition()
    {
        if (SelectedPosition != null)
        {
            await DeletePositionWithParameter(SelectedPosition);
        }
    }

    private async Task DeletePositionWithParameter(Position? position)
    {
        if (position != null)
        {
            var result = MessageBox.Show($"Are you sure you want to delete the position '{position.Name}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    IsLoading = true;
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var positionToDelete = await context.Positions.FindAsync(position.Id);
                    if (positionToDelete != null)
                    {
                        context.Positions.Remove(positionToDelete);
                        await context.SaveChangesAsync();
                        LoadPositions();
                        _notificationService.ShowSuccess("Position deleted successfully!");
                    }
                }
                catch (Exception ex)
                {
                    Message = $"Error deleting position: {ex.Message}";
                    _notificationService.ShowError($"Failed to delete position: {ex.Message}");
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }
    }

    private async void ToggleActiveStatus(Position? position)
    {
        if (position != null)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var positionToUpdate = await context.Positions.FindAsync(position.Id);
                if (positionToUpdate != null)
                {
                    positionToUpdate.IsActive = !positionToUpdate.IsActive;
                    positionToUpdate.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();

                    // Update the local collection
                    position.IsActive = positionToUpdate.IsActive;
                    position.UpdatedAt = positionToUpdate.UpdatedAt;
                    
                    var statusText = position.IsActive ? "activated" : "deactivated";
                    _notificationService.ShowSuccess($"Position '{position.Name}' {statusText}!");
                }
            }
            catch (Exception ex)
            {
                Message = $"Error toggling position status: {ex.Message}";
                _notificationService.ShowError($"Failed to update position status: {ex.Message}");
            }
        }
    }
}