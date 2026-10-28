using Microsoft.AspNetCore.Antiforgery;

namespace OCasaGrande.Web.Security;

public sealed class ValidateAntiforgeryFilter(IAntiforgery antiforgery) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!await antiforgery.IsRequestValidAsync(context.HttpContext))
        {
            return TypedResults.Problem(
                title: "Token de segurança inválido. Recarregue a página e tente novamente.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return await next(context);
    }
}
