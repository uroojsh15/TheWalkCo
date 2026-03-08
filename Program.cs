using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TheWalkco.Data;
using TheWalkco.Hubs;
using TheWalkco.Interfaces;
using TheWalkco.Repositories;
using TheWalkco.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

//builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<ApplicationDbContext>();


builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddSingleton<IEmailSender, NoOpEmailSender>();
builder.Services.AddSignalR();

// ?? Identity WITHOUT roles
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Home/AccessDenied";
});


// ?? POLICY-BASED + CLAIM-BASED AUTHORIZATION
builder.Services.AddAuthorization(options =>
{
    // Admin = authenticated AND email contains "admin"
    options.AddPolicy("AdminAccess", policy =>
        policy.RequireAssertion(ctx =>
        {
            var identity = ctx.User?.Identity;
            if (identity == null || !identity.IsAuthenticated)
                return false;

            var email =
                ctx.User.FindFirst(ClaimTypes.Email)?.Value ??
                ctx.User.Identity?.Name ??
                string.Empty;

            return email.Contains("admin", StringComparison.OrdinalIgnoreCase);
        }));

    // User = authenticated AND NOT admin
    options.AddPolicy("UserAccess", policy =>
        policy.RequireAssertion(ctx =>
        {
            var identity = ctx.User?.Identity;
            if (identity == null || !identity.IsAuthenticated)
                return false;

            var email =
                ctx.User.FindFirst(ClaimTypes.Email)?.Value ??
                ctx.User.Identity?.Name ??
                string.Empty;

            return !email.Contains("admin", StringComparison.OrdinalIgnoreCase);
        }));
});

// Identity with roles
//builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
//    options.SignIn.RequireConfirmedAccount = false)   // you can set false for testing
// .AddEntityFrameworkStores<ApplicationDbContext>()
// .AddDefaultTokenProviders();
//builder.Services.ConfigureApplicationCookie(options =>
//{
//    options.LoginPath = "/Identity/Account/Login";
//    options.AccessDeniedPath = "/Home/AccessDenied";// Correct default path for Identity UI
//});

//builder.Services.AddAuthorization(options =>
//{
//    options.AddPolicy("AdminOnly", policy =>
//        policy.RequireRole("Admin"));  // checks if user has Admin role
//});

//// Optional policy using EmailVerified claim
//builder.Services.AddAuthorization(options =>
//{
//    options.AddPolicy("EmailVerifiedOnly", policy =>
//        policy.RequireClaim("EmailVerified", "true"));
//});
/*builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("Role", "Admin"));
});*/


// MVC + Razor Pages (needed for Identity UI & MapRazorPages)
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddRazorPages();


var app = builder.Build();

//// Seed roles and admin user
//using (var scope = app.Services.CreateScope())
//{
//    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
//    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

//    string[] roles = { "Admin", "User" };

//    foreach (var role in roles)
//    {
//        if (!await roleManager.RoleExistsAsync(role))
//        {
//            await roleManager.CreateAsync(new IdentityRole(role));
//        }
//    }

//    string adminEmail = "admin@gmail.com";
//    string adminPassword = "Admin@123";

//    var adminUser = await userManager.FindByEmailAsync(adminEmail);
//    if (adminUser == null)
//    {
//        adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail };
//        var result = await userManager.CreateAsync(adminUser, adminPassword);

//        if (result.Succeeded)
//        {
//            await userManager.AddToRoleAsync(adminUser, "Admin");
//        }
//    }

//    var adminClaims = await userManager.GetClaimsAsync(adminUser);
//    if (!adminClaims.Any(c => c.Type == "EmailVerified"))
//    {
//        await userManager.AddClaimAsync(adminUser, new Claim("EmailVerified", "true"));
//    }
//}
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    string adminEmail = "admin@gmail.com";
    string adminPassword = "Admin@123";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);

        if (result.Succeeded)
        {
            // ?? Add CLAIM instead of ROLE
            await userManager.AddClaimAsync(adminUser,
                new Claim("Role", "Admin"));
        }
    }
}
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.MapHub<OrderHub>("/orderHub");
app.MapHub<ProductHub>("/productHub");




app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
