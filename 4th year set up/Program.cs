using Doctor.Repository;
using _4th_year_set_up.Services;
using admin.Repository;
using LabManager.DataAccess;
using LabManager.Repositories;
using LabManager.Repository;
using Microsoft.Data.SqlClient;
using Patient.Repository;
using Rotativa.AspNetCore;


var builder = WebApplication.CreateBuilder(args);
// FirstLoginPasswordFilter: users with a temporary password must change it
// before they can open any other page (spec: change password at first login).
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add<_4th_year_set_up.Filters.FirstLoginPasswordFilter>());
// PDFs need wwwroot\Rotativa\wkhtmltopdf.exe; start without it if it's missing
if (File.Exists(Path.Combine(builder.Environment.WebRootPath, "Rotativa", "wkhtmltopdf.exe")))
    RotativaConfiguration.Setup(builder.Environment.WebRootPath, "Rotativa");

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

// Doctor module (real NMB_HaematologyLab schema/stored procedures — see
// Doctor/DataAccess/DoctorDataAccess.cs)
builder.Services.AddScoped<DoctorRepository>(provider =>
    new DoctorRepository(
        builder.Configuration.GetConnectionString("conn")!));


//Lab Manager
builder.Services.AddTransient<ISqlDataAcess, SqlDataAccess>();
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