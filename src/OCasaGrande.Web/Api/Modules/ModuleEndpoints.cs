using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using OCasaGrande.Web.Auth;
using OCasaGrande.Web.Data;
using OCasaGrande.Web.Security;

namespace OCasaGrande.Web.Api.Modules;

public static class ModuleEndpoints
{
    public static IEndpointRouteBuilder MapModulesApi(this IEndpointRouteBuilder app)
    {
        var modules = app.MapGroup("/api/admin/modules")
            .RequireAuthorization(AdminAuthentication.AdminPolicy)
            .RequireRateLimiting(RateLimiting.ApiPolicy);

        modules.MapGet("/", ListModules);
        modules.MapPut("/{key:regex(^[a-z0-9-]{{1,50}}$)}", UpdateModule)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();

        return app;
    }

    private static async Task<Ok<List<ModuleItem>>> ListModules(AppDbContext db, CancellationToken cancellationToken) =>
        TypedResults.Ok(await db.Modules.AsNoTracking()
            .OrderBy(m => m.Id)
            .Select(m => new ModuleItem(m.Key, m.Name, m.Description, m.IsEnabled, m.UpdatedAt))
            .ToListAsync(cancellationToken));

    private static async Task<Results<Ok<ModuleItem>, ValidationProblem, ProblemHttpResult>> UpdateModule(
        string key, UpdateModuleRequest request, AppDbContext db, CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var module = await db.Modules.SingleOrDefaultAsync(m => m.Key == key, cancellationToken);
        if (module is null)
        {
            return TypedResults.Problem(title: "Módulo não encontrado.", statusCode: StatusCodes.Status404NotFound);
        }

        module.IsEnabled = request.IsEnabled!.Value;
        module.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new ModuleItem(module.Key, module.Name, module.Description, module.IsEnabled, module.UpdatedAt));
    }
}
