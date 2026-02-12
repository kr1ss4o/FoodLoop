using Microsoft.AspNetCore.Identity;

namespace FoodLoop.Data.Seed
{
    public static class RoleSeeder
    {
        private static readonly string[] Roles = new[]
        {
            "Admin",
            "Restaurant",
            "Client"
        };

        public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
    }
}