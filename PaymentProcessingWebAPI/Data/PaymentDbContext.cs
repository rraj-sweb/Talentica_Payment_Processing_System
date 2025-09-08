using Microsoft.EntityFrameworkCore;
using PaymentProcessingWebAPI.Models.Entities;

namespace PaymentProcessingWebAPI.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<PaymentMethod> PaymentMethods { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Order configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CustomerId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Currency).HasMaxLength(3).HasDefaultValue("USD");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.OrderNumber).IsUnique();
        });

        // Transaction configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TransactionId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.AuthorizeNetTransactionId).HasMaxLength(100);
            entity.Property(e => e.ResponseCode).HasMaxLength(10);
            entity.Property(e => e.ResponseMessage).HasMaxLength(500);
            
            entity.HasOne(e => e.Order)
                  .WithMany(e => e.Transactions)
                  .HasForeignKey(e => e.OrderId);
        });

        // PaymentMethod configuration
        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LastFourDigits).IsRequired().HasMaxLength(4);
            entity.Property(e => e.CardType).HasMaxLength(20);
            entity.Property(e => e.NameOnCard).HasMaxLength(100);
            entity.Property(e => e.BillingAddress).HasMaxLength(500);
            
            entity.HasOne(e => e.Order)
                  .WithOne(e => e.PaymentMethod)
                  .HasForeignKey<PaymentMethod>(e => e.OrderId);
        });
    }
}
