using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using OCasaGrande.Web.Api.Modules;
using OCasaGrande.Web.Api.SecurityEvents;
using OCasaGrande.Web.Auth;
using OCasaGrande.Web.Data;
using OCasaGrande.Web.Security;
using OCasaGrande.Web.Site;

if (args is [PasswordHashCommand.Name])
{
    return PasswordHashCommand.Run();
}

var builder = WebApplication.CreateBuilder(args);

var database = DatabaseConnection.Resolve(builder.Configuration);
builder.Services.AddDbContext<AppDbContext>(options => AppDbContext.Configure(options, database.Application));

builder.Services.AddDataProtection()
    .SetApplicationName("OCasaGrande")
    .PersistKeysToDbContext<AppDbContext>();

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("postgres");
builder.Services.AddAppRateLimiting(builder.Configuration);
builder.Services.AddScoped<SharedRateLimiter>();
builder.Services.AddScoped<SecurityAudit>();
builder.Services.AddAdminAuthentication(builder.Configuration, builder.Environment);

var app = builder.Build();

DatabaseInitializer.Migrate(database);

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
app.MapSecurityEventsApi();
app.MapHealthChecks("/health");

app.Run();
return 0;

public partial class Program;
