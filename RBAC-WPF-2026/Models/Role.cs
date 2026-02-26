using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RBAC_WPF_2026.Models;

public class Role
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    [NotMapped]
    public IEnumerable<User> Users => UserRoles.Select(ur => ur.User);

    [NotMapped]
    public IEnumerable<Permission> Permissions => RolePermissions.Select(rp => rp.Permission);
}