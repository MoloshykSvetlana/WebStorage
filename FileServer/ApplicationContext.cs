using FileServer.Models;
using Microsoft.EntityFrameworkCore;

namespace FileServer;

public class ApplicationContext : DbContext
{
    public DbSet<FileModel> Files { get; set; }

    public DbSet<UserModel> Users { get; set; }

    public DbSet<AccessModel> Accesses { get; set; }

    public DbSet<RoleModel> Roles { get; set; }


    public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
    {
        Database.EnsureCreated();
    }

    public void DetachEntities<TEntity>(IEnumerable<TEntity> entities)
    {
        foreach (var entity in entities)
        {
            DetachEntity(entity);
        }
    }

    public void DetachEntity<TEntity>(TEntity entity)
    {
        Entry(entity).State = EntityState.Detached;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RoleModel>()
            .HasData(new List<RoleModel>
            {
                new()
                {
                    Id = 1,
                    Name = "Admin"
                },
                new()
                {
                    Id = 2,
                    Name = "User"
                }
            });

        modelBuilder.Entity<UserModel>()
            .HasData(new List<UserModel>
            {
                new()
                {
                    Id = 1,
                    Login = "admin",
                    Password = "admin",
                    RoleModelId = 1
                },
                new()
                {
                    Id = 2,
                    Login = "user",
                    Password = "user",
                    RoleModelId = 2
                },
                new()
                {
                    Id = 3,
                    Login = "user1",
                    Password = "user1",
                    RoleModelId = 2
                }
            });

        /*modelBuilder.Entity<FileModel>()
            .HasData(new List<FileModel>
            {
                new()
                {
                    Id = 1,
                    Name = "file1",
                    UserModelId = 2,
                    Bytes = new byte[] {1,2,3},
                    ShareToAll = 1
                },
                new()
                {
                    Id = 2,
                    Name = "file2",
                    UserModelId = 3,
                    Bytes = new byte[] {1,2,3},
                    ShareToAll = 2
                }
            });

        modelBuilder.Entity<AccessModel>()
            .HasData(new List<AccessModel>
            {
                new()
                {
                    Id = 3,
                    UserModelId = 3,
                    FileModelId = 1,
                    AccessLevel = 1
                }
            });*/
    }
}
