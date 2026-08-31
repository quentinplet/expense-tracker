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
    public DbSet<RecurringTransaction> RecurringTransactions { get; set; }

    public static readonly Guid MemberRoleId = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid AdminRoleId = new("22222222-2222-2222-2222-222222222222");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresEnum<Frequency>();
        modelBuilder.HasPostgresEnum<TransactionType>();

        // Mapping explicite des propriétés (FORCE le type de colonne)
        modelBuilder.Entity<RecurringTransaction>()
            .Property(x => x.Frequency)
            .HasColumnType("frequency");

        modelBuilder.Entity<RecurringTransaction>()
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

            // Détacher plutôt que supprimer : une transaction déjà générée reste
            // un fait financier indépendant si la transaction récurrente est supprimée.
            e.HasOne(t => t.RecurringTransaction)
             .WithMany(r => r.Transactions)
             .HasForeignKey(t => t.RecurringTransactionId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.Property(c => c.Type).HasColumnType("transaction_type");
            e.Property(c => c.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(c => new { c.UserId, c.Name, c.Type }).IsUnique();
        });

        modelBuilder.Entity<Budget>(e =>
        {
            e.Property(b => b.AmountLimit).HasPrecision(18, 2);
            e.Property(b => b.Month).HasMaxLength(7).IsRequired();

            // Deux index uniques partiels plutôt qu'un seul : Postgres traite chaque
            // NULL comme distinct des autres dans un index unique classique, donc
            // (UserId, Month, CategoryId) seul laisserait passer plusieurs budgets
            // globaux le même mois pour le même utilisateur.
            e.HasIndex(b => new { b.UserId, b.Month, b.CategoryId })
             .IsUnique()
             .HasFilter("\"CategoryId\" IS NOT NULL");
            e.HasIndex(b => new { b.UserId, b.Month })
             .IsUnique()
             .HasFilter("\"CategoryId\" IS NULL");

            e.HasOne(b => b.Category)
             .WithMany(c => c.Budgets)
             .HasForeignKey(b => b.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RecurringTransaction>(e =>
        {
            e.Property(r => r.Amount).HasPrecision(18, 2);
            e.Property(r => r.Label).HasMaxLength(200).IsRequired();

            e.HasOne(r => r.Category)
             .WithMany(c => c.RecurringTransactions)
             .HasForeignKey(r => r.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IdentityRole<Guid>>()
            .HasData(
            new IdentityRole<Guid> { Id = MemberRoleId, Name = "Member", NormalizedName = "MEMBER", ConcurrencyStamp = "a" },
            new IdentityRole<Guid> { Id = AdminRoleId, Name = "Admin", NormalizedName = "ADMIN", ConcurrencyStamp = "b" }
        );

    }

}