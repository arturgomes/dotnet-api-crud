using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            // Add index on email for faster lookups
            entity.HasIndex(e => e.Email);
        });

        // Seed data
        var now = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<User>().HasData(
        new User(
            Id: new Guid("c398344e-a745-4221-a472-74d1a011030e"), // Static GUID 1
            Name: "Jane Doe", 
            Email: "jane.doe@example.com", 
            CreatedAt: now, 
            UpdatedAt: now),
            
        new User(
            Id: new Guid("d68840b3-f726-4074-a690-349479b47e27"), // Static GUID 2
            Name: "John Smith", 
            Email: "john.smith@example.com", 
            CreatedAt: now, 
            UpdatedAt: now)
    );
    }
}