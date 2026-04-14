namespace ExpenseTrackerApi.Data;

using ExpenseTrackerApi.Models;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Expense> Expenses => Set<Expense>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        builder.Entity<User>()
            .Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(80);

        builder.Entity<Expense>()
            .Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(120);
    }
}
