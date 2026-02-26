using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RBAC_WPF_2026.Commands;
using RBAC_WPF_2026.Data;
using RBAC_WPF_2026.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace RBAC_WPF_2026.ViewModels;

public class UserManagementViewModel : BaseViewModel
{
    private readonly AppDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private User? _selectedUser;
    private string _message = string.Empty;
    private bool _isLoading;

    public ObservableCollection<User> Users { get; set; } = new();

    public User? SelectedUser
    {
        get => _selectedUser;
        set => SetProperty(ref _selectedUser, value);
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

    public ICommand AddUserCommand { get; }
    public ICommand EditUserCommand { get; }
    public ICommand DeleteUserCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand EditUserContextCommand { get; }
    public ICommand DeleteUserContextCommand { get; }

    public UserManagementViewModel(AppDbContext context, IServiceProvider serviceProvider)
    {
        _context = context;
        _serviceProvider = serviceProvider;

        AddUserCommand = new RelayCommand(AddUser);
        EditUserCommand = new RelayCommand(EditUser, () => SelectedUser != null);
        DeleteUserCommand = new RelayCommand(DeleteUser, () => SelectedUser != null);
        RefreshCommand = new RelayCommand(LoadUsers);
        EditUserContextCommand = new RelayCommand(param => EditUserWithParameter(param as User));
        DeleteUserContextCommand = new RelayCommand(param => DeleteUserWithParameter(param as User));

        LoadUsers();
    }

    private async void LoadUsers()
    {
        try
        {
            IsLoading = true;
            Message = "Loading users...";
            
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var users = await context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ToListAsync();

            Users.Clear();
            foreach (var user in users)
            {
                Users.Add(user);
            }

            Message = $"Loaded {users.Count} users.";
        }
        catch (Exception ex)
        {
            Message = $"Error loading users: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void AddUser()
    {
        var dialog = new UserEditViewModel(_context, _serviceProvider);
        if (ShowUserDialog(dialog) == true)
        {
            LoadUsers();
            Message = "User added successfully.";
        }
    }

    private void EditUser()
    {
        if (SelectedUser == null) return;

        var dialog = new UserEditViewModel(_context, _serviceProvider, SelectedUser);
        if (ShowUserDialog(dialog) == true)
        {
            LoadUsers();
            Message = "User updated successfully.";
        }
    }

    private async void DeleteUser()
    {
        if (SelectedUser == null) return;

        var result = MessageBox.Show(
            $"Are you sure you want to delete user '{SelectedUser.Username}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                var userToDelete = await context.Users.FindAsync(SelectedUser.Id);
                if (userToDelete != null)
                {
                    context.Users.Remove(userToDelete);
                    await context.SaveChangesAsync();
                    LoadUsers();
                    Message = "User deleted successfully.";
                }
            }
            catch (Exception ex)
            {
                Message = $"Error deleting user: {ex.Message}";
            }
        }
    }

    private bool? ShowUserDialog(UserEditViewModel viewModel)
    {
        var dialog = new Views.UserEditDialog
        {
            DataContext = viewModel,
            Owner = Application.Current.MainWindow
        };

        return dialog.ShowDialog();
    }

    private void EditUserWithParameter(User? user)
    {
        if (user == null) return;

        var dialog = new UserEditViewModel(_context, _serviceProvider, user);
        if (ShowUserDialog(dialog) == true)
        {
            LoadUsers();
            Message = "User updated successfully.";
        }
    }

    private async void DeleteUserWithParameter(User? user)
    {
        if (user == null) return;

        var result = MessageBox.Show(
            $"Are you sure you want to delete user '{user.Username}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var userToDelete = await context.Users.FindAsync(user.Id);
                if (userToDelete != null)
                {
                    context.Users.Remove(userToDelete);
                    await context.SaveChangesAsync();
                    LoadUsers();
                    Message = "User deleted successfully.";
                }
            }
            catch (Exception ex)
            {
                Message = $"Error deleting user: {ex.Message}";
            }
        }
    }
}