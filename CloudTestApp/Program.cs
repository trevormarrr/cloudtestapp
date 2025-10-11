using System;
using CloudTestApp.Data;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// Razor Pages
builder.Services.AddRazorPages();

// ---- Heroku: bind to provided PORT before Build ----
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// ---- Resolve MySQL connection string in this order ----
// 1) ConnectionStrings:Default (Heroku: set with ConnectionStrings__Default)
// 2) MYSQL_CONNECTION (optional env you might set)
// 3) Parse JAWSDB_URL (Heroku add-on default)
string? connStr =
    config.GetConnectionString("Default") ??
    Environment.GetEnvironmentVariable("MYSQL_CONNECTION");

if (string.IsNullOrWhiteSpace(connStr))
{
    var jawsUrl = Environment.GetEnvironmentVariable("JAWSDB_URL");
    if (!string.IsNullOrWhiteSpace(jawsUrl))
    {
        connStr = BuildMySqlConnectionStringFromUrl(jawsUrl);
    }
}

if (string.IsNullOrWhiteSpace(connStr))
{
    throw new InvalidOperationException(
        "No MySQL connection string found. Set ConnectionStrings:Default, MYSQL_CONNECTION, or JAWSDB_URL."
    );
}

// ---- Use explicit server version to avoid AutoDetect at startup ----
// Most JawsDB free/low tiers are MySQL 5.7. If yours is 8.0, change to (8,0,0).
var serverVersion = new MySqlServerVersion(new Version(5, 7, 0));

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(connStr!, serverVersion, mySql =>
    {
        mySql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        );
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// ---- Apply migrations on startup (don’t take the app down if it fails) ----
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        // Log and continue so the dyno doesn’t crash on first boot while you debug DB access.
        app.Logger.LogError(ex, "Database migration failed at startup.");
        // If you prefer hard-fail, rethrow here.
        // throw;
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.Run();

// --------- helpers ---------
static string BuildMySqlConnectionStringFromUrl(string url)
{
    var uri = new Uri(url);
    var userInfo = uri.UserInfo.Split(':', 2);
    var user = Uri.UnescapeDataString(userInfo[0]);
    var pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";

    var csb = new MySqlConnectionStringBuilder
    {
        Server = uri.Host,
        Port = (uint)(uri.Port > 0 ? uri.Port : 3306),
        Database = uri.LocalPath.Trim('/'),
        UserID = user,
        Password = pass,
        SslMode = MySqlSslMode.Preferred,
        AllowPublicKeyRetrieval = true
    };

    return csb.ConnectionString;
}
