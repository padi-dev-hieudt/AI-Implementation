# RBAC WPF Application

A comprehensive WPF .NET 6 application implementing Role-Based Access Control (RBAC) using MVVM pattern and Entity Framework Core.

## Features

- **User Management**: Create, read, update, and delete users
- **Role Management**: Create, read, update, and delete roles
- **Permission Management**: Create, read, update, and delete permissions
- **Role Assignment**: Assign multiple roles to users
- **Permission Assignment**: Assign multiple permissions to roles
- **Password Hashing**: Secure password storage using BCrypt
- **Data Validation**: Input validation with user-friendly error messages
- **MVVM Pattern**: Clean separation of concerns with ViewModels and Views

## Database Schema

The application uses the following database tables:

### Users
- `Id` (int, PK)
- `Username` (nvarchar(100), unique)
- `Email` (nvarchar(255), unique)
- `PasswordHash` (nvarchar(255))
- `IsActive` (bit, default true)
- `CreatedAt` (datetime2, default GETUTCDATE())

### Roles
- `Id` (int, PK)
- `Name` (nvarchar(100), unique)
- `Description` (nvarchar(500))

### Permissions
- `Id` (int, PK)
- `Name` (nvarchar(100), unique)
- `Description` (nvarchar(500))

### UserRoles (Many-to-Many)
- `UserId` (FK Users)
- `RoleId` (FK Roles)

### RolePermissions (Many-to-Many)
- `RoleId` (FK Roles)
- `PermissionId` (FK Permissions)

## Project Structure

```
RBAC-WPF-2026/
├── Data/
│   └── AppDbContext.cs           # Entity Framework DbContext
├── Models/
│   ├── User.cs                   # User entity model
│   ├── Role.cs                   # Role entity model
│   ├── Permission.cs             # Permission entity model
│   ├── UserRole.cs               # User-Role junction table
│   └── RolePermission.cs         # Role-Permission junction table
├── ViewModels/
│   ├── BaseViewModel.cs          # Base class for ViewModels
│   ├── MainWindowViewModel.cs    # Main window navigation
│   ├── UserManagementViewModel.cs    # User CRUD operations
│   ├── UserEditViewModel.cs          # User add/edit dialog
│   ├── RoleManagementViewModel.cs    # Role CRUD operations
│   ├── RoleEditViewModel.cs          # Role add/edit dialog
│   ├── PermissionManagementViewModel.cs  # Permission CRUD operations
│   └── PermissionEditViewModel.cs        # Permission add/edit dialog
├── Views/
│   ├── UserManagementView.xaml       # User management UI
│   ├── UserEditDialog.xaml           # User add/edit dialog UI
│   ├── RoleManagementView.xaml       # Role management UI
│   ├── RoleEditDialog.xaml           # Role add/edit dialog UI
│   ├── PermissionManagementView.xaml # Permission management UI
│   ├── PermissionEditDialog.xaml     # Permission add/edit dialog UI
│   └── Converters/                   # Value converters for UI
├── Commands/
│   └── RelayCommand.cs               # ICommand implementation
├── App.xaml.cs                       # Application startup and DI configuration
├── MainWindow.xaml                   # Main application window
└── appsettings.json                  # Configuration file
```

## Getting Started

### Prerequisites

- .NET 6.0 or later
- Visual Studio 2022 or VS Code
- SQL Server or LocalDB

### Installation

1. **Clone or download the project**

2. **Update the connection string** in `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=RBAC_DB;Trusted_Connection=true;MultipleActiveResultSets=true"
     }
   }
   ```

3. **Restore packages**:
   ```bash
   dotnet restore
   ```

4. **Build the project**:
   ```bash
   dotnet build
   ```

5. **Run the application**:
   ```bash
   dotnet run
   ```

### Database Setup

The application will automatically:
- Create the database if it doesn't exist
- Apply the schema using Entity Framework Code First
- Seed sample data including:
  - Default permissions (Read, Write, Delete, Admin)
  - Sample roles (Administrator, User, Manager)
  - Sample users (admin/admin123, user/user123)

## Usage

### User Management

1. **View Users**: The Users tab displays all users in a DataGrid
2. **Add User**: Click "Add User" to create a new user with username, email, password, and role assignments
3. **Edit User**: Select a user and click "Edit User" to modify user details and role assignments
4. **Delete User**: Select a user and click "Delete User" to remove the user
5. **Role Assignment**: During add/edit, select checkboxes to assign roles to users

### Role Management

1. **View Roles**: The Roles tab displays all roles with their permissions
2. **Add Role**: Click "Add Role" to create a new role with name, description, and permission assignments
3. **Edit Role**: Select a role and click "Edit Role" to modify role details and permission assignments
4. **Delete Role**: Select a role and click "Delete Role" to remove the role
5. **Permission Assignment**: During add/edit, select checkboxes to assign permissions to roles

### Permission Management

1. **View Permissions**: The Permissions tab displays all permissions
2. **Add Permission**: Click "Add Permission" to create a new permission with name and description
3. **Edit Permission**: Select a permission and click "Edit Permission" to modify permission details
4. **Delete Permission**: Select a permission and click "Delete Permission" to remove the permission

## Key Features

### Security
- Passwords are hashed using BCrypt before storage
- Email and username uniqueness validation
- Input validation with user-friendly error messages

### User Experience
- Clean, modern UI with consistent styling
- Real-time validation feedback
- Success/error message display
- Intuitive navigation between management screens

### Data Integrity
- Foreign key constraints ensure referential integrity
- Unique constraints prevent duplicate usernames/emails/role names/permission names
- Cascading deletes properly handle related record cleanup

### MVVM Implementation
- Clean separation between UI and business logic
- INotifyPropertyChanged implementation for data binding
- Command pattern for user actions
- Dependency injection for service management

## Customization

### Adding New Fields
1. Update the corresponding model class
2. Create and run Entity Framework migration
3. Update ViewModels to handle new fields
4. Update XAML views to display new fields

### Styling
- Modify the styles in XAML files for custom appearance
- Update color schemes in the ResourceDictionary
- Add custom templates for enhanced UI controls

### Business Logic
- Extend ViewModels for additional functionality
- Add new commands for custom operations
- Implement additional validation rules

## Troubleshooting

### Database Issues
- Verify SQL Server/LocalDB is running
- Check connection string in appsettings.json
- Delete database and restart to recreate with fresh schema

### Build Issues
- Ensure .NET 6.0 SDK is installed
- Run `dotnet restore` to restore NuGet packages
- Check for missing using statements or namespace issues

### Runtime Issues
- Check Output window for Entity Framework logs
- Verify all required assemblies are referenced
- Ensure proper dependency injection configuration

## License

This project is provided as an educational example for implementing RBAC in WPF applications.