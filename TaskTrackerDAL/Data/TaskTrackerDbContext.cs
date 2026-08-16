using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using TaskTrackerDAL.Models;


namespace TaskTrackerDAL.Data
{
    public class TaskTrackerDbContext : DbContext
    {
        public TaskTrackerDbContext(DbContextOptions<TaskTrackerDbContext> options) : base(options)
        {

        }
        public DbSet<Company> Companies => Set<Company>();//1

        public DbSet<User> Users => Set<User>();//2

        public DbSet<Role> Roles => Set<Role>();//3

        public DbSet<UserRole> UserRoles => Set<UserRole>();//4

        public DbSet<Project> Projects => Set<Project>();//5

        public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();//6

        public DbSet<ProjectTask> Tasks => Set<ProjectTask>();//7

        public DbSet<TaskProgressNote> Notes=> Set<TaskProgressNote>();//8

        public DbSet<Notification> Notifications => Set<Notification>();//9

        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();//10

        public DbSet<TaskFile> TaskFiles => Set<TaskFile>();//11

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---------- Company ----------
            modelBuilder.Entity<Company>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
                entity.Property(c => c.Email).HasMaxLength(200);
                entity.Property(c => c.Phone).HasMaxLength(50);
                entity.Property(c => c.Address).HasMaxLength(500);
            });

            // ---------- Role ----------
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(r => r.Name).IsUnique();
                entity.Property(r => r.Description).HasMaxLength(300);
            });

            // ---------- User ----------
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.FullName).IsRequired().HasMaxLength(200);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(200);
                entity.Property(u => u.UserName).IsRequired().HasMaxLength(100);
                entity.Property(u => u.PasswordHash).IsRequired();

                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.UserName).IsUnique();

                // Company (1) --< (many) User
                entity.HasOne(u => u.Company)
                      .WithMany(c => c.Users)
                      .HasForeignKey(u => u.CompanyId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- UserRole (join entity, composite key) ----------
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(ur => new { ur.UserId, ur.RoleId });

                entity.HasOne(ur => ur.User)
                      .WithMany(u => u.UserRoles)
                      .HasForeignKey(ur => ur.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ur => ur.Role)
                      .WithMany(r => r.UserRoles)
                      .HasForeignKey(ur => ur.RoleId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------- Project ----------
            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
                entity.Property(p => p.Description).HasMaxLength(1000);
                entity.Property(p => p.Status).HasConversion<int>();
                entity.Property(p => p.Status).HasConversion<int>();

                // Company (1) --< (many) Project
                entity.HasOne(p => p.Company)
                      .WithMany(c => c.Projects)
                      .HasForeignKey(p => p.CompanyId)
                      .OnDelete(DeleteBehavior.Restrict);

                // User (1, CreatedBy) --< (many) Project
                entity.HasOne(p => p.CreatedByUser)
                      .WithMany(u => u.CreatedProjects)
                      .HasForeignKey(p => p.CreatedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- ProjectMember (join entity, composite key) ----------
            modelBuilder.Entity<ProjectMember>(entity =>
            {
                entity.HasKey(pm => new { pm.ProjectId, pm.UserId });

                entity.HasOne(pm => pm.Project)
                      .WithMany(p => p.ProjectMembers)
                      .HasForeignKey(pm => pm.ProjectId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pm => pm.User)
                      .WithMany(u => u.ProjectMemberships)
                      .HasForeignKey(pm => pm.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------- ProjectTask ----------
            modelBuilder.Entity<ProjectTask>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Title).IsRequired().HasMaxLength(200);
                entity.Property(t => t.Description).HasMaxLength(1000);
                entity.Property(t => t.Status).HasConversion<int>();
                entity.Property(t => t.Priority).HasConversion<int>();

                // Project (1) --< (many) ProjectTask
                entity.HasOne(t => t.Project)
                      .WithMany(p => p.Tasks)
                      .HasForeignKey(t => t.ProjectId)
                      .OnDelete(DeleteBehavior.Restrict);

                // User (1, AssignedTo, optional) --< (many) ProjectTask
                entity.HasOne(t => t.AssignedToUser)
                      .WithMany(u => u.AssignedTasks)
                      .HasForeignKey(t => t.AssignedToUserId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);

                // User (1, CreatedBy) --< (many) ProjectTask
                entity.HasOne(t => t.CreatedByUser)
                      .WithMany(u => u.CreatedTasks)
                      .HasForeignKey(t => t.CreatedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            //Project Task Notes
            modelBuilder.Entity<TaskProgressNote>(entity =>
            {
                entity.HasKey(n => n.Id);
                entity.Property(n => n.Note).IsRequired().HasMaxLength(2000);

                entity.HasOne(n => n.Task)
                      .WithMany()
                      .HasForeignKey(n => n.TaskId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(n => n.AuthorUser)
                      .WithMany()
                      .HasForeignKey(n => n.AuthorUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            


// ---------- Notification ----------
modelBuilder.Entity<Notification>(entity =>
{
    entity.HasKey(n => n.Id);
    entity.Property(n => n.Message).IsRequired().HasMaxLength(500);
    entity.Property(n => n.Type).HasConversion<int>();

    entity.HasOne(n => n.RecipientUser)
          .WithMany()
          .HasForeignKey(n => n.RecipientUserId)
          .OnDelete(DeleteBehavior.Cascade);

    entity.HasOne(n => n.RelatedTask)
          .WithMany()
          .HasForeignKey(n => n.RelatedTaskId)
          .OnDelete(DeleteBehavior.SetNull)
          .IsRequired(false);

    entity.HasOne(n => n.RelatedProject)
          .WithMany()
          .HasForeignKey(n => n.RelatedProjectId)
          .OnDelete(DeleteBehavior.SetNull)
          .IsRequired(false);

    entity.HasIndex(n => new { n.RecipientUserId, n.IsRead });
});

            // ---------- AuditLog ----------
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.ModelName).IsRequired().HasMaxLength(100);
                entity.Property(a => a.ActionType).HasConversion<int>();
                entity.Property(a => a.FieldName).HasMaxLength(100);
                entity.Property(a => a.OldValue).HasMaxLength(2000);
                entity.Property(a => a.NewValue).HasMaxLength(2000);

                entity.HasOne(a => a.PerformedByUser)
                      .WithMany()
                      .HasForeignKey(a => a.PerformedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(a => new { a.ModelName, a.ModelId });
            });
            //------TaskFiles----------
            modelBuilder.Entity<TaskFile>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.FileName)
                      .HasMaxLength(255)
                      .IsRequired();

                entity.Property(x => x.StoredFileName)
                      .HasMaxLength(255)
                      .IsRequired();

                entity.Property(x => x.FilePath)
                      .HasMaxLength(500)
                      .IsRequired();

                entity.HasOne(x => x.Task)
                      .WithMany(t => t.TaskFiles)
                      .HasForeignKey(x => x.TaskId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
