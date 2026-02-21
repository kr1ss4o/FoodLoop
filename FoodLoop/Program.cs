using FoodLoop.Data;
using FoodLoop.Data.Seed;
using FoodLoop.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services
    .AddIdentity<User, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Cookie paths
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Home/AccessDenied";
});

var app = builder.Build();

// Error handling
if (!app.Environment.IsDevelopment())
{
    // Ако няма ErrorController, може да се смени на статична страница или да се добави ErrorController.
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ✅ Default route = Offers/Index (Home става Offers)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Offers}/{action=Index}/{id?}"
);

// Seeding (ако тези класове съществуват при Педал)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<User>>();

        await RoleSeeder.SeedRolesAsync(roleManager);
        await AdminSeeder.SeedAdminAsync(userManager, roleManager);

        await DatabaseSeeder.SeedAsync(services);
    }
    catch (Exception ex)
    {
        Console.WriteLine("SEEDING ERROR: " + ex.Message);
    }
}

app.Run();