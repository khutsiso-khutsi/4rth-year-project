using _4th_year_set_up.Services;
using admin.Repository;
using LabManager.DataAccess;
using LabManager.Models;
using LabManager.Repositories;
using LabManager.Repository;
using Microsoft.Data.SqlClient;
using Patient.Repository;
using Rotativa.AspNetCore;


var builder = WebApplication.CreateBuilder(args);
// Ensure environment variables are included so secrets can be provided outside appsettings.json
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddControllersWithViews();
//RotativaConfiguration.Setup(builder.Environment.WebRootPath, "Rotativa");

// ✅ Changed from AddSingleton to AddScoped


builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Patient class library
builder.Services.AddScoped<UserRepository>(provider =>
    new UserRepository(
        builder.Configuration.GetConnectionString("DefaultConnection")!));

// Admin class library
builder.Services.AddScoped<AdminRepository>(provider =>
    new AdminRepository(
        builder.Configuration.GetConnectionString("DefaultConnection")!));


builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
//Lab Manager
builder.Services.AddTransient<ISqlDataAcess ,SqlDataAccess>(); 
builder.Services.AddTransient<IConsumablesRepository, ConsumablesRepository>();
builder.Services.AddTransient<IOrderRepository, ConsumableOrderRepository>();
builder.Services.AddTransient<ITestCatalogueRepository, TestCatalogueRepository>();
builder.Services.AddTransient<IStaffRepository, StaffRepository>();
builder.Services.AddTransient<IEmailService, LabManager.Repository.EmailService>();
// Register the concrete EmailService used by HomeController so the DI container can resolve it
builder.Services.AddTransient<_4th_year_set_up.Services.EmailService>();




var app = builder.Build();


SqlConnection.ClearAllPools();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


