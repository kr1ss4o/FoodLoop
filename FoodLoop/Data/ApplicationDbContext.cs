using FoodLoop.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FoodLoop.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Restaurant> Restaurants { get; set; }
        public DbSet<Offer> Offers { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<OfferTag> OfferTags { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<ReservationItem> ReservationItems { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Many-to-Many: Offer - Tag
            builder.Entity<OfferTag>()
                .HasKey(o => new { o.OfferId, o.TagId });

            builder.Entity<OfferTag>()
                .HasOne(o => o.Offer)
                .WithMany(o => o.OfferTags)
                .HasForeignKey(o => o.OfferId);

            builder.Entity<OfferTag>()
                .HasOne(o => o.Tag)
                .WithMany(t => t.OfferTags)
                .HasForeignKey(o => o.TagId);
        }
    }
}