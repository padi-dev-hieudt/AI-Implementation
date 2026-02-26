using RBAC_WPF_2026.Commands;
using RBAC_WPF_2026.Data;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;

namespace RBAC_WPF_2026.ViewModels;

public class MainWindowViewModel : BaseViewModel
{
    private BaseViewModel? _currentViewModel;
    private readonly IServiceProvider _serviceProvider;

    public BaseViewModel? CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    public ICommand ShowUsersCommand { get; }
    public ICommand ShowRolesCommand { get; }
    public ICommand ShowPermissionsCommand { get; }
    public ICommand ShowPositionsCommand { get; }

    public MainWindowViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        ShowUsersCommand = new RelayCommand(() => {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            CurrentViewModel = new UserManagementViewModel(context, _serviceProvider);
        });
        ShowRolesCommand = new RelayCommand(() => {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            CurrentViewModel = new RoleManagementViewModel(context, _serviceProvider);
        });
        ShowPermissionsCommand = new RelayCommand(() => {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            CurrentViewModel = new PermissionManagementViewModel(context, _serviceProvider);
        });
        ShowPositionsCommand = new RelayCommand(() => {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<Services.NotificationService>();
            CurrentViewModel = new PositionManagementViewModel(context, _serviceProvider, notificationService);
        });

        // Set initial view
        using var initialScope = _serviceProvider.CreateScope();
        var initialContext = initialScope.ServiceProvider.GetRequiredService<AppDbContext>();
        CurrentViewModel = new UserManagementViewModel(initialContext, _serviceProvider);
    }
}