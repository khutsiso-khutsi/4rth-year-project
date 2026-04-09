using Patient.Repository;
using admin.Repository;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
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