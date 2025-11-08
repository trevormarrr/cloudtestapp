using System;
using CloudTestApp.Data;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Serilog;
// If LogActionFilter is in a namespace, make sure to include it, e.g.:
using CloudTestApp.Filters;

var builder = WebApplication.CreateBuilder(args);

// ---- Serilog to stdout (platforms collect stdout) ----
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithThreadId()
    .Enrich.WithEnvironmentUserName()
    .WriteTo.Console(outputTemplate:
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({SourceContext}) {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Make environment variables available via Configuration
builder.Configuration.AddEnvironmentVariables();

// Bind to provided PORT (Heroku-style) if present
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// ---- DB wiring: Dev (in-memory) vs Non-Dev (MySQL) ----
var useInMemory =
    builder.Environment.IsDevelopment() &&
    builder.Configuration.GetValue<bool>("UseInMemoryDb");

if (useInMemory)
{
    // Development: no MySQL required
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("Dev"));
}
else
{
    // ---- Resolve MySQL connection string in this order ----
    // 1) MYSQL_CONNECTION
    // 2) ConnectionStrings:Default
    // 3) JAWSDB_URL (Heroku add-on)
    string? connStr =
        Environment.GetEnvironmentVariable("MYSQL_CONNECTION")
        ?? builder.Configuration.GetConnectionString("Default");

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
            "No MySQL connection string found. Set MYSQL_CONNECTION, ConnectionStrings:Default, or JAWSDB_URL."
        );
    }

    // Auto-detect MySQL server version (ok in cloud; can hardcode if desired)
    var serverVersion = ServerVersion.AutoDetect(connStr);

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseMySql(connStr, serverVersion, mySql =>
            mySql.EnableRetryOnFailure(maxRetryCount: 5,
                                       maxRetryDelay: TimeSpan.FromSeconds(10),
                                       errorNumbersToAdd: null)));
}

// ---- MVC/Razor + global action filter ----
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<LogActionFilter>(); // ensure using/import for this type
});

var app = builder.Build();

// ---- Global exception guard ----
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("GlobalException");
        logger.LogError(ex, "Unhandled exception");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Internal Server Error");
    }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapControllers(); // safe even if you have none yet

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
        // Cloud DBs expect TLS; Azure MySQL requires SSL
        SslMode = MySqlSslMode.Required,
        AllowPublicKeyRetrieval = true
    };

    return csb.ConnectionString;
}
