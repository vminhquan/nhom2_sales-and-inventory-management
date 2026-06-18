using Microsoft.EntityFrameworkCore;
using nhom2.Domain.Entities;

namespace nhom2.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình quan hệ giữa Order và OrderItem
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Order>()
                .HasOne(order => order.Customer)
                .WithMany(customer => customer.Orders)
                .HasForeignKey(order => order.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Order>()
                .Property(order => order.DiscountAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(order => order.AmountPaid)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(order => order.PaymentMethod)
                .HasConversion<string>()
                .HasMaxLength(20);

            modelBuilder.Entity<PaymentTransaction>()
                .HasOne(payment => payment.Order)
                .WithOne(order => order.Payment)
                .HasForeignKey<PaymentTransaction>(payment => payment.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PaymentTransaction>()
                .HasIndex(payment => payment.OrderCode)
                .IsUnique();

            modelBuilder.Entity<OrderItem>()
                .Property(item => item.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Customer>()
                .HasIndex(customer => customer.Phone)
                .IsUnique();

            modelBuilder.Entity<ChatSession>()
                .HasIndex(session => new { session.CustomerUserId, session.IsActive });

            modelBuilder.Entity<ChatMessage>()
                .HasOne(message => message.Session)
                .WithMany(session => session.Messages)
                .HasForeignKey(message => message.ChatSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
