using _4th_year_set_up.Services;
using admin.Repository;
using LabManager.DataAccess;
using LabManager.Repositories;
using LabManager.Repository;
using Microsoft.Data.SqlClient;
using Patient.Repository;
using Rotativa.AspNetCore;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
//RotativaConfiguration.Setup(builder.Environment.WebRootPath, "Rotativa");

// ✅ Changed from AddSingleton to AddScoped
builder.Services.AddScoped<EmailService>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Patient class library
builder.Services.AddScoped<UserRepository>(provider =>
    new UserRepository(
        builder.Configuration.GetConnectionString("conn")!));

// Admin class library
builder.Services.AddScoped<AdminRepository>(provider =>
    new AdminRepository(
        builder.Configuration.GetConnectionString("conn")!));


//Lab Manager
builder.Services.AddTransient<ISqlDataAcess ,SqlDataAccess>(); 
builder.Services.AddTransient<IConsumablesRepository, ConsumablesRepository>();
builder.Services.AddTransient<IOrderRepository, ConsumableOrderRepository>();
builder.Services.AddTransient<ITestCategoryrepository, TestCategoryRepository>();
builder.Services.AddTransient<IStaffRepository, StaffRepository>();


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


