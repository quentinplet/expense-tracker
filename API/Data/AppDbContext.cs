using System;
using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{

    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<TransactionType> TransactionTypes { get; set; }
    public DbSet<Budget> Budgets { get; set; }
    public DbSet<SavedTransaction> SavedTransactions { get; set; }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresEnum<Frequency>();
        modelBuilder.HasPostgresEnum<TransactionTypeName>();

        // 2. Mapping explicite des propriétés (FORCE le type de colonne)
        modelBuilder.Entity<SavedTransaction>()
            .Property(x => x.Frequency)
            .HasColumnType("frequency");

        modelBuilder.Entity<TransactionType>()
            .Property(x => x.Name)
            .HasColumnType("transaction_type_name");

        // Empêcher la suppression d'une catégorie si elle a des transactions
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Category)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Empêcher la suppression d'un type si il a des catégories
        modelBuilder.Entity<Category>()
            .HasOne(c => c.TransactionType)
            .WithMany(t => t.Categories)
            .HasForeignKey(c => c.TransactionTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<IdentityRole>()
            .HasData(
            new IdentityRole { Id = "member-id", Name = "Member", NormalizedName = "MEMBER", ConcurrencyStamp = "a" },
            new IdentityRole { Id = "admin-id", Name = "Admin", NormalizedName = "ADMIN", ConcurrencyStamp = "b" }
        );

    }

}
