using CloudTestApp.Data;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure; // keep this

var builder = WebApplication.CreateBuilder(args);

// Razor Pages
builder.Services.AddRazorPages();

// Get connection from env first, otherwise appsettings
var conn = Environment.GetEnvironmentVariable("MYSQL_CONNECTION")
          ?? builder.Configuration.GetConnectionString("Default");

// Bind to Heroku PORT (must be before Build)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// ✅ Use an explicit server version to avoid AutoDetect (which opens a connection during startup)
var serverVersion = new MySqlServerVersion(new Version(5, 7, 0)); // JawsDB free is usually 5.7.x

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(conn, serverVersion,
        mySqlOptions => mySqlOptions.CharSetBehavior(CharSetBehavior.NeverAppend)
    )
);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Apply migrations on startup (creates tables if not present)
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
