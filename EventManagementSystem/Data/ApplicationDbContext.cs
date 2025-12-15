using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EventManagementSystem.Models;

namespace EventManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Tables
        public new DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<EventCategory> EventCategories { get; set; }
        public DbSet<Venue> Venues { get; set; }
        public DbSet<EventImage> EventImages { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<LoyaltyPoint> LoyaltyPoints { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<LoyaltyHistory> LoyaltyHistories { get; set; }
        public DbSet<Payment> Payments { get; set; }

        public DbSet<Ticket> Tickets { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Role).HasDefaultValue("customer");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Table mapping
            modelBuilder.Entity<EventCategory>().ToTable("event_categories");
            modelBuilder.Entity<Venue>().ToTable("venues");
            modelBuilder.Entity<Event>().ToTable("events");
            modelBuilder.Entity<EventImage>().ToTable("event_images");
            modelBuilder.Entity<Booking>().ToTable("bookings");
            modelBuilder.Entity<LoyaltyPoint>().ToTable("loyalty_points");
            modelBuilder.Entity<Promotion>().ToTable("promotions");

            // Event configuration
            modelBuilder.Entity<Event>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Title).HasColumnName("title");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.EventDate).HasColumnName("event_date");
                entity.Property(e => e.StartTime).HasColumnName("start_time");
                entity.Property(e => e.EndTime).HasColumnName("end_time");
                entity.Property(e => e.TicketPrice).HasColumnName("ticket_price");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.TotalCapacity).HasColumnName("total_capacity");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.CategoryId).HasColumnName("category_id");
                entity.Property(e => e.VenueId).HasColumnName("venue_id");
                entity.Property(e => e.OrganizerId).HasColumnName("organizer_id");

                entity.HasMany(e => e.EventImages)
                      .WithOne(img => img.Event)
                      .HasForeignKey(img => img.EventId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Bookings)
                      .WithOne(b => b.Event)
                      .HasForeignKey(b => b.EventId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // EventImage configuration
            modelBuilder.Entity<EventImage>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.EventId).HasColumnName("event_id");
                entity.Property(e => e.ImageUrl).HasColumnName("image_url").IsRequired();
            });

            // Booking configuration
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.ToTable("bookings");

                entity.Property(b => b.Id).HasColumnName("id");
                entity.Property(b => b.CustomerId).HasColumnName("customer_id");
                entity.Property(b => b.EventId).HasColumnName("event_id");
                entity.Property(b => b.NumberOfTickets).HasColumnName("number_of_tickets");
                entity.Property(b => b.TicketPrice).HasColumnName("ticket_price");
                entity.Property(b => b.TotalAmount).HasColumnName("total_amount");
                entity.Property(b => b.DiscountAmount).HasColumnName("discount_amount");
                entity.Property(b => b.FinalAmount).HasColumnName("final_amount"); // ✅ map correctly
                entity.Property(b => b.PromotionCode).HasColumnName("promotion_code");
                entity.Property(b => b.LoyaltyUsed).HasColumnName("loyalty_used");
                entity.Property(b => b.LoyaltyEarned).HasColumnName("loyalty_earned");
                entity.Property(b => b.PaymentStatus).HasColumnName("payment_status");
                entity.Property(b => b.BookingStatus).HasColumnName("booking_status");
                entity.Property(b => b.CreatedAt)
                      .HasColumnName("created_at")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(b => b.Customer)
                      .WithMany()
                      .HasForeignKey(b => b.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(b => b.Tickets)
                      .WithOne(t => t.Booking)
                      .HasForeignKey(t => t.BookingId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.ToTable("tickets");
                entity.HasKey(t => t.Id);

                entity.Property(t => t.Id).HasColumnName("id");
                entity.Property(t => t.BookingId).HasColumnName("booking_id");

                entity.Property(t => t.TicketNumber)
                      .HasColumnName("ticket_number")
                      .IsRequired();

                entity.Property(t => t.QRCode)
                      .HasColumnName("qr_code")
                      .IsRequired();

                entity.Property(t => t.Status)
                      .HasColumnName("status")
                      .HasDefaultValue("valid");

                entity.Property(t => t.CreatedAt)
                      .HasColumnName("created_at")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(t => t.UpdatedAt)
                      .HasColumnName("updated_at")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP")
                      .ValueGeneratedOnAddOrUpdate();

                entity.HasOne(t => t.Booking)
                      .WithMany(b => b.Tickets)
                      .HasForeignKey(t => t.BookingId)
                      .OnDelete(DeleteBehavior.Cascade);
            });




            // LoyaltyPoint configuration
            modelBuilder.Entity<LoyaltyPoint>(entity =>
            {
                entity.Property(l => l.Id).HasColumnName("id");
                entity.Property(l => l.CustomerId).HasColumnName("customer_id");
                entity.Property(l => l.Points).HasColumnName("points");
                entity.Property(l => l.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Promotion configuration
            modelBuilder.Entity<Promotion>(entity =>
            {
                entity.Property(p => p.Id).HasColumnName("id");
                entity.Property(p => p.Code).HasColumnName("code").IsRequired();
                entity.Property(p => p.DiscountType).HasColumnName("discount_type").IsRequired();
                entity.Property(p => p.DiscountValue).HasColumnName("discount_value");
                entity.Property(p => p.StartDate).HasColumnName("start_date");
                entity.Property(p => p.EndDate).HasColumnName("end_date");
                entity.Property(p => p.Status).HasColumnName("status").HasDefaultValue("active");
            });

            // LoyaltyHistory configuration
            modelBuilder.Entity<LoyaltyHistory>(entity =>
            {
                entity.ToTable("loyalty_history");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.CustomerId).HasColumnName("customer_id").IsRequired();
                entity.Property(e => e.BookingId).HasColumnName("booking_id").IsRequired(false);
                entity.Property(e => e.ChangeType).HasColumnName("change_type").IsRequired();
                entity.Property(e => e.Points).HasColumnName("points").IsRequired();
                entity.Property(e => e.CreatedAt)
                      .HasColumnName("created_at")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Indexes
                entity.HasIndex(e => e.CustomerId);
                entity.HasIndex(e => e.BookingId);

                // Relationships
                entity.HasOne<Booking>()
                      .WithMany()
                      .HasForeignKey(e => e.BookingId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<User>()
                      .WithMany()
                      .HasForeignKey(e => e.CustomerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.ToTable("payments");

                entity.Property(p => p.Id).HasColumnName("id");
                entity.Property(p => p.BookingId).HasColumnName("booking_id");
                entity.Property(p => p.Amount).HasColumnName("amount");
                entity.Property(p => p.PaymentMethod).HasColumnName("payment_method");
                entity.Property(p => p.TransactionId).HasColumnName("transaction_id");
                entity.Property(p => p.Status).HasColumnName("status").HasDefaultValue("pending");
                entity.Property(p => p.CreatedAt)
                      .HasColumnName("created_at")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(p => p.Booking)
                      .WithMany()
                      .HasForeignKey(p => p.BookingId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(p => p.BookingId);
            });


        }
    }
}
