using Patient.Repository;
using admin.Repository;
using _4th_year_set_up.Services;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();

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
        builder.Configuration.GetConnectionString("DefaultConnection")!));

// Admin class library
builder.Services.AddScoped<AdminRepository>(provider =>
    new AdminRepository(
        builder.Configuration.GetConnectionString("DefaultConnection")!));

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
    pattern: "{controller=ManagerDashboard}/{action=Index}/{id?}");

app.Run();