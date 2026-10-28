using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using OCasaGrande.Web.Api.Modules;
using OCasaGrande.Web.Auth;
using OCasaGrande.Web.Data;
using OCasaGrande.Web.Security;
using OCasaGrande.Web.Site;

if (args is [PasswordHashCommand.Name])
{
    return PasswordHashCommand.Run();
}

var builder = WebApplication.CreateBuilder(args);

var databasePath = DatabaseInitializer.ResolvePath(builder.Configuration, builder.Environment);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(DatabaseInitializer.ConnectionString(databasePath)));

if (builder.Configuration["DataProtection:KeysPath"] is { Length: > 0 } keysPath)
{
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddAppRateLimiting(builder.Configuration);
builder.Services.AddAdminAuthentication(builder.Configuration, builder.Environment);

var app = builder.Build();

DatabaseInitializer.Migrate(databasePath);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseStatusCodePages();
app.UseSecurityHeaders();
app.UseRateLimiter();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapSitePages();
app.MapAuthApi();
app.MapModulesApi();
app.MapHealthChecks("/health");

app.Run();
return 0;

public partial class Program;
