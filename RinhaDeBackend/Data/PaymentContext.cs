using Microsoft.EntityFrameworkCore;
using RinhaDeBackend.Models;

namespace RinhaDeBackend.Data
{
    public class PaymentContext : DbContext
    {
        public PaymentContext(DbContextOptions<PaymentContext> options) : base(options) { }

        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.ToTable("Payments");

                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("Id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.CorrelationId)
                    .HasColumnName("CorrelationId")
                    .IsRequired();

                entity.Property(e => e.Amount)
                    .HasColumnName("Amount")
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(e => e.RequestedAt)
                    .HasColumnName("RequestedAt")
                    .IsRequired();

                entity.Property(e => e.ProcessedAt)
                    .HasColumnName("ProcessedAt")
                    .IsRequired();

                entity.Property(e => e.ProcessorType)
                    .HasColumnName("ProcessorType")
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.Success)
                    .HasColumnName("Success")
                    .IsRequired();

                entity.Property(e => e.ErrorMessage)
                    .HasColumnName("ErrorMessage")
                    .HasMaxLength(500)
                    .IsRequired(false);

                entity.HasIndex(e => e.CorrelationId)
                    .IsUnique()
                    .HasDatabaseName("IX_Payments_CorrelationId");

                entity.HasIndex(e => e.ProcessedAt)
                    .HasDatabaseName("IX_Payments_ProcessedAt");

                entity.HasIndex(e => e.ProcessorType)
                    .HasDatabaseName("IX_Payments_ProcessorType");

                entity.HasIndex(e => new { e.ProcessorType, e.ProcessedAt })
                    .HasDatabaseName("IX_Payments_ProcessorType_ProcessedAt");
            });
        }
    }
}
