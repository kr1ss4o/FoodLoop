using FoodLoop.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodLoop.Data.Seed
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Apply migrations automatically
            await context.Database.MigrateAsync();

            // ---------------------------
            // SEED CATEGORIES
            // ---------------------------
            if (!context.Categories.Any())
            {
                context.Categories.AddRange(new[]
                {
                    new Category { Name = "Pizza" },
                    new Category { Name = "Sushi" },
                    new Category { Name = "Burgers" },
                    new Category { Name = "Dessert" },
                    new Category { Name = "Drinks" },
                    new Category { Name = "Healthy" }
                });

                await context.SaveChangesAsync();
            }

            // ---------------------------
            // SEED TAGS
            // ---------------------------
            if (!context.Tags.Any())
            {
                context.Tags.AddRange(new[]
                {
                    new Tag { Name = "Spicy" },
                    new Tag { Name = "Vegan" },
                    new Tag { Name = "Budget" },
                    new Tag { Name = "Family size" },
                    new Tag { Name = "Grill" },
                    new Tag { Name = "Low sugar" }
                });

                await context.SaveChangesAsync();
            }

            // ---------------------------
            // GET TEST BUSINESS USER
            // ---------------------------
            var businessUser = await context.Users
                .FirstOrDefaultAsync(u => u.Email == "business@foodloop.com");

            if (businessUser == null)
            {
                Console.WriteLine("⚠ No business user found. Seed skipped for restaurant + offers.");
                return;
            }

            // ---------------------------
            // SEED RESTAURANT (if missing)
            // ---------------------------
            var restaurant = await context.Restaurants
                .FirstOrDefaultAsync(r => r.OwnerUserId == businessUser.Id);

            if (restaurant == null)
            {
                restaurant = new Restaurant
                {
                    Id = Guid.NewGuid(),
                    Name = "Bella Italia",
                    BusinessEmail = "contact@bellaitalia.com",
                    Phone = "0888123456",
                    Address = "Sofia, Vitosha Blvd 101",
                    Rating = 4.8,
                    OwnerUserId = businessUser.Id,
                    CreatedAt = DateTime.UtcNow
                };

                context.Restaurants.Add(restaurant);
                await context.SaveChangesAsync();
            }

            // ---------------------------
            // SEED SAMPLE OFFERS
            // ---------------------------
            if (!context.Offers.Any(o => o.RestaurantId == restaurant.Id))
            {
                var pizzaCategory = await context.Categories.FirstAsync(c => c.Name == "Pizza");
                var sushiCategory = await context.Categories.FirstAsync(c => c.Name == "Sushi");

                var offer1 = new Offer
                {
                    Id = Guid.NewGuid(),
                    Title = "Pepperoni Pizza",
                    Description = "Classic pepperoni with fresh mozzarella.",
                    OriginalPrice = 18.90m,
                    DiscountedPrice = 12.90m,
                    QuantityAvailable = 20,
                    CategoryId = pizzaCategory.Id,
                    RestaurantId = restaurant.Id,
                    EndsAt = DateTime.UtcNow.AddDays(2),
                    ImageUrl = "/images/offers/default1.jpg"
                };

                var offer2 = new Offer
                {
                    Id = Guid.NewGuid(),
                    Title = "Salmon Sushi Box",
                    Description = "Fresh salmon sushi combo.",
                    OriginalPrice = 22.90m,
                    DiscountedPrice = 15.90m,
                    QuantityAvailable = 15,
                    CategoryId = sushiCategory.Id,
                    RestaurantId = restaurant.Id,
                    EndsAt = DateTime.UtcNow.AddDays(3),
                    ImageUrl = "/images/offers/default2.jpg"
                };

                context.Offers.AddRange(offer1, offer2);
                await context.SaveChangesAsync();

                // ---------------------------
                // TAG ASSIGNMENT
                // ---------------------------
                var spicy = await context.Tags.FirstAsync(t => t.Name == "Spicy");
                var vegan = await context.Tags.FirstAsync(t => t.Name == "Vegan");

                context.OfferTags.AddRange(new[]
                {
                    new OfferTag { OfferId = offer1.Id, TagId = spicy.Id },
                    new OfferTag { OfferId = offer2.Id, TagId = vegan.Id }
                });

                await context.SaveChangesAsync();
            }

            Console.WriteLine("✔ Database seeding completed!");
        }
    }
}