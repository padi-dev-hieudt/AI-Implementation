using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RBAC_WPF_2026.Commands;
using RBAC_WPF_2026.Data;
using RBAC_WPF_2026.Models;
using RBAC_WPF_2026.Services;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Input;

namespace RBAC_WPF_2026.ViewModels;

public class UserEditViewModel : BaseViewModel
{
    private readonly AppDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly User? _originalUser;
    private int _id;
    private string _username = string.Empty;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private bool _isActive = true;
    private string _message = string.Empty;
    private string _errorMessage = string.Empty;

    public bool IsEditMode => _originalUser != null;
    public string Title => IsEditMode ? "Edit User" : "Add User";

    public int Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
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

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public ObservableCollection<RoleSelectionItem> AvailableRoles { get; } = new();

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public Action? CloseAction { get; set; }
    public event Action? UserSaved;

    public UserEditViewModel(AppDbContext context, IServiceProvider serviceProvider, User? user = null)
    {
        _context = context;
        _serviceProvider = serviceProvider;
        _originalUser = user;

        SaveCommand = new RelayCommand(Save, CanSave);
        CancelCommand = new RelayCommand(Cancel);

        if (user != null)
        {
            Id = user.Id;
            Username = user.Username;
            Email = user.Email;
            IsActive = user.IsActive;
        }

        LoadRolesAsync();
    }

    private async void LoadRolesAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var roles = await context.Roles.ToListAsync();
            var userRoleIds = new HashSet<int>();
            
            // If editing an existing user, get their roles
            if (_originalUser != null)
            {
                var userWithRoles = await context.Users
                    .Include(u => u.UserRoles)
                    .FirstOrDefaultAsync(u => u.Id == _originalUser.Id);
                userRoleIds = userWithRoles?.UserRoles
                    .Select(ur => ur.RoleId)
                    .ToHashSet() ?? new HashSet<int>();
            }

            AvailableRoles.Clear();
            foreach (var role in roles)
            {
                AvailableRoles.Add(new RoleSelectionItem
                {
                    Role = role,
                    IsSelected = userRoleIds.Contains(role.Id)
                });
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading roles: {ex.Message}";
        }
    }

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(Email) &&
               (IsEditMode || !string.IsNullOrWhiteSpace(Password));
    }

    private async void Save()
    {
        try
        {
            Message = string.Empty;
            ErrorMessage = string.Empty;

            // Validate email format
            if (!new EmailAddressAttribute().IsValid(Email))
            {
                ErrorMessage = "Please enter a valid email address.";
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (IsEditMode)
            {
                // Update existing user
                var user = await context.Users.FindAsync(Id);
                if (user != null)
                {
                    user.Username = Username;
                    user.Email = Email;
                    user.IsActive = IsActive;

                    if (!string.IsNullOrWhiteSpace(Password))
                    {
                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password);
                    }

                    await context.SaveChangesAsync();
                    
                    // Update user roles
                    await UpdateUserRoles(context, user);
                    
                    Message = "User updated successfully!";
                    NotificationService.Instance.ShowSuccess("User updated successfully!");
                    OnUserSaved();
                }
            }
            else
            {
                // Check if username already exists
                if (await context.Users.AnyAsync(u => u.Username == Username))
                {
                    ErrorMessage = "Username already exists.";
                    return;
                }

                // Check if email already exists
                if (await context.Users.AnyAsync(u => u.Email == Email))
                {
                    ErrorMessage = "Email already exists.";
                    return;
                }

                // Create new user
                var user = new User
                {
                    Username = Username,
                    Email = Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
                    IsActive = IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                context.Users.Add(user);
                await context.SaveChangesAsync();
                
                // Update user roles
                await UpdateUserRoles(context, user);
                
                Message = "User created successfully!";
                NotificationService.Instance.ShowSuccess("User created successfully!");
                OnUserSaved();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error saving user: {ex.Message}";
        }
    }

    private void OnUserSaved()
    {
        UserSaved?.Invoke();
        CloseAction?.Invoke();
    }

    private async Task UpdateUserRoles(AppDbContext context, User user)
    {
        // Remove existing roles
        var existingUserRoles = context.UserRoles.Where(ur => ur.UserId == user.Id);
        context.UserRoles.RemoveRange(existingUserRoles);

        // Add selected roles
        var selectedRoleIds = AvailableRoles.Where(r => r.IsSelected).Select(r => r.Role.Id);
        foreach (var roleId in selectedRoleIds)
        {
            context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
        }

        await context.SaveChangesAsync();
    }

    private void Cancel()
    {
        CloseAction?.Invoke();
    }
}

public class RoleSelectionItem : BaseViewModel
{
    private bool _isSelected;

    public Role Role { get; set; } = null!;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}