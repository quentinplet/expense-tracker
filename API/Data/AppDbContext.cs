using System;
using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{

    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Budget> Budgets { get; set; }
    public DbSet<SavedTransaction> SavedTransactions { get; set; }

    public static readonly Guid MemberRoleId = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid AdminRoleId = new("22222222-2222-2222-2222-222222222222");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresEnum<Frequency>();
        modelBuilder.HasPostgresEnum<TransactionType>();

        // Mapping explicite des propriétés (FORCE le type de colonne)
        modelBuilder.Entity<SavedTransaction>()
            .Property(x => x.Frequency)
            .HasColumnType("frequency");

        modelBuilder.Entity<SavedTransaction>()
            .Property(x => x.Type)
            .HasColumnType("transaction_type");

        modelBuilder.Entity<Transaction>(e =>
        {
            e.Property(t => t.Type).HasColumnType("transaction_type");
            e.Property(t => t.Amount).HasPrecision(18, 2);
            e.Property(t => t.Label).HasMaxLength(200).IsRequired();

            e.HasIndex(t => new { t.UserId, t.Date });
            e.HasIndex(t => new { t.UserId, t.CategoryId, t.Date });

            // Empêcher la suppression d'une catégorie si elle a des transactions
            e.HasOne(t => t.Category)
             .WithMany(c => c.Transactions)
             .HasForeignKey(t => t.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.Property(c => c.Type).HasColumnType("transaction_type");
            e.Property(c => c.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(c => new { c.UserId, c.Name }).IsUnique();
        });

        modelBuilder.Entity<Budget>(e =>
        {
            e.Property(b => b.Amount).HasPrecision(18, 2);
            e.HasIndex(b => new { b.UserId, b.Year, b.Month, b.CategoryId }).IsUnique();

            e.HasOne(b => b.Category)
             .WithMany(c => c.Budgets)
             .HasForeignKey(b => b.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SavedTransaction>()
            .Property(s => s.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<IdentityRole<Guid>>()
            .HasData(
            new IdentityRole<Guid> { Id = MemberRoleId, Name = "Member", NormalizedName = "MEMBER", ConcurrencyStamp = "a" },
            new IdentityRole<Guid> { Id = AdminRoleId, Name = "Admin", NormalizedName = "ADMIN", ConcurrencyStamp = "b" }
        );

    }

}