using CloudTestApp.Data;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure; // ok to keep

var builder = WebApplication.CreateBuilder(args);

// Razor Pages
builder.Services.AddRazorPages();

// Get connection (Heroku env first, else appsettings)
var conn = Environment.GetEnvironmentVariable("MYSQL_CONNECTION")
          ?? builder.Configuration.GetConnectionString("Default");

// Bind to Heroku PORT (before Build)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Use explicit server version to avoid AutoDetect at startup
var serverVersion = new MySqlServerVersion(new Version(5, 7, 0)); // JawsDB free is usually 5.7

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(conn, serverVersion));  // <-- removed CharSetBehavior line

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.Run();
