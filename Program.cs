using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WellnessPlatform.Components;
using WellnessPlatform.Data;
using WellnessPlatform.Models;
using WellnessPlatform.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Entity Framework and Identity
builder.Services.AddDbContext<WellnessContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<ApplicationUser>(options => 
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddEntityFrameworkStores<WellnessContext>();

// Register custom services
builder.Services.AddScoped<TreatmentRecommendationService>();
builder.Services.AddScoped<DataValidationService>();
builder.Services.AddScoped<AuthorizationService>();
builder.Services.AddScoped<BiomarkerStatusService>();
builder.Services.AddScoped<CorrelationAnalysisService>();
builder.Services.AddScoped<TreatmentEffectivenessService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map Identity UI pages
app.MapRazorPages();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<WellnessContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    await DataSeeder.SeedDataAsync(context, userManager);
}

app.Run();
