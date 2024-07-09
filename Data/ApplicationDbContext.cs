using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // MODEL TANIMLAMALARI
    public DbSet<User> Users { get; set; }
    public DbSet<Receipt> Receipts { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<BaseUser> BaseUsers { get; set; }

    //TABLOLAR ARASI İLİŞKİLER
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User and BaseUser one-to-one relationship
        modelBuilder.Entity<User>()
            .HasOne(u => u.BaseUser)
            .WithOne(b => b.User)
            .HasForeignKey<BaseUser>(b => b.UserId);

        // User and Receipts one-to-many relationship
        modelBuilder.Entity<User>()
            .HasMany(u => u.Receipts)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId);

        // Receipt and Products one-to-many relationship
        modelBuilder.Entity<Receipt>()
            .HasMany(r => r.Products)
            .WithOne(p => p.Receipt)
            .HasForeignKey(p => p.ReceiptId);
    }
}
