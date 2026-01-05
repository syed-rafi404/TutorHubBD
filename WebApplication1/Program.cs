using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using TutorHubBD.Web.Configuration;
using TutorHubBD.Web.Data;
using TutorHubBD.Web.Models;
using TutorHubBD.Web.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => 
    options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddRazorPages();

// Configure Stripe Settings
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("StripeSettings"));

// Register HttpClientFactory for AI Search
builder.Services.AddHttpClient();

// Register application services
builder.Services.AddScoped<ITuitionOfferService, TuitionOfferService>();
builder.Services.AddScoped<TuitionRequestService>();
builder.Services.AddScoped<ICommissionService, CommissionService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IAiSearchService, AiSearchService>();

var app = builder.Build();

// Seed roles and admin user
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    
    // Create roles if they don't exist
    string[] roles = { "Admin", "Guardian", "Teacher" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
    
    // Seed Admin User
    var adminEmail = "rafitheflash@gmail.com";
    var adminPassword = "rafitheflash@gmail.comA1";
    
    var existingUser = await userManager.FindByEmailAsync(adminEmail);
    
    if (existingUser != null)
    {
        // Check if user is already admin
        var isAdmin = await userManager.IsInRoleAsync(existingUser, "Admin");
        if (!isAdmin)
        {
            // Remove from other roles and add to Admin
            var currentRoles = await userManager.GetRolesAsync(existingUser);
            await userManager.RemoveFromRolesAsync(existingUser, currentRoles);
            await userManager.AddToRoleAsync(existingUser, "Admin");
        }
        
        // Reset password to ensure it matches
        var token = await userManager.GeneratePasswordResetTokenAsync(existingUser);
        await userManager.ResetPasswordAsync(existingUser, token, adminPassword);
    }
    else
    {
        // Create new admin user
        var adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "System Administrator",
            EmailConfirmed = true,
            Address = "",
            Bio = ""
        };
        
        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
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
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
