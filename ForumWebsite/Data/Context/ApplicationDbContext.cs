using ForumWebsite.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ForumWebsite.Data.Context
{
    /// <summary>
    /// EF Core DbContext.  All table configuration is done here via Fluent API
    /// (preferred over data annotations for a cleaner domain model).
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User>     Users      { get; set; } = null!;
        public DbSet<Post>     Posts      { get; set; } = null!;
        public DbSet<Comment>  Comments   { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Tag>      Tags       { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ─── User ────────────────────────────────────────────────────────────
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Username)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(e => e.Email)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.PasswordHash)
                      .IsRequired();

                entity.Property(e => e.Role)
                      .IsRequired()
                      .HasMaxLength(20)
                      .HasDefaultValue(UserRoles.User);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                // Do NOT use HasDefaultValue(true) here.
                // EF Core skips sending bool columns when their value equals the CLR default (false),
                // relying on the DB default instead. This would make it impossible to explicitly
                // persist IsActive = false for a new record. The entity initializer (= true)
                // is sufficient; EF will always send the value explicitly.
                // entity.Property(e => e.IsActive).HasDefaultValue(true);  // intentionally removed

                // Unique constraints — enforced at DB level as well as application level
                entity.HasIndex(e => e.Username).IsUnique().HasDatabaseName("UX_Users_Username");
                entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("UX_Users_Email");
            });

            // ─── Category ─────────────────────────────────────────────────────────
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.Description)
                      .HasMaxLength(500);

                entity.Property(e => e.IsDefault)
                      .HasDefaultValue(false);

                entity.Property(e => e.SortOrder)
                      .HasDefaultValue(0);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(e => e.Name).IsUnique().HasDatabaseName("UX_Categories_Name");
            });

            // ─── Tag ──────────────────────────────────────────────────────────────
            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(e => e.Name).IsUnique().HasDatabaseName("UX_Tags_Name");
            });

            // ─── Post ─────────────────────────────────────────────────────────────
            modelBuilder.Entity<Post>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Title)
                      .IsRequired()
                      .HasMaxLength(300);

                entity.Property(e => e.Content)
                      .IsRequired();           // nvarchar(max)

                entity.Property(e => e.ViewCount)
                      .HasDefaultValue(0);

                entity.Property(e => e.IsDeleted)
                      .HasDefaultValue(false);

                entity.Property(e => e.IsClosed)
                      .HasDefaultValue(false);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                // Restrict: deleting a user does NOT cascade-delete their posts
                entity.HasOne(e => e.User)
                      .WithMany(u => u.Posts)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Restrict: cannot delete a category that still has posts
                entity.HasOne(e => e.Category)
                      .WithMany(c => c.Posts)
                      .HasForeignKey(e => e.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Many-to-many Post ↔ Tag via implicit PostTags join table.
                // The join table column names EF generates are PostsId/TagsId —
                // we index TagsId so "posts by tag" queries hit an index, not a scan.
                entity.HasMany(e => e.Tags)
                      .WithMany(t => t.Posts)
                      .UsingEntity(j =>
                      {
                          j.ToTable("PostTags");
                          j.HasIndex("TagsId").HasDatabaseName("IX_PostTags_TagId");
                      });

                // Indexes to speed up common list/filter queries
                entity.HasIndex(e => e.UserId).HasDatabaseName("IX_Posts_UserId");
                entity.HasIndex(e => e.CategoryId).HasDatabaseName("IX_Posts_CategoryId");
                entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_Posts_CreatedAt");
                entity.HasIndex(e => e.IsDeleted).HasDatabaseName("IX_Posts_IsDeleted");
            });

            // ─── Comment ──────────────────────────────────────────────────────────
            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Content)
                      .IsRequired()
                      .HasMaxLength(5000);

                entity.Property(e => e.IsDeleted)
                      .HasDefaultValue(false);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                // Cascade: if a post is hard-deleted, its comments are removed too
                entity.HasOne(e => e.Post)
                      .WithMany(p => p.Comments)
                      .HasForeignKey(e => e.PostId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Restrict: deleting a user does NOT cascade-delete their comments
                entity.HasOne(e => e.User)
                      .WithMany(u => u.Comments)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.PostId).HasDatabaseName("IX_Comments_PostId");
                entity.HasIndex(e => e.UserId).HasDatabaseName("IX_Comments_UserId");
            });
        }
    }
}
