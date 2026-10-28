namespace OCasaGrande.Web.Api.Modules;

public sealed record ModuleItem(string Key, string Name, string Description, bool IsEnabled, DateTimeOffset UpdatedAt);

public sealed record UpdateModuleRequest(bool? IsEnabled)
{
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();

        if (IsEnabled is null)
        {
            errors[nameof(IsEnabled)] = ["Informe se o módulo deve ficar ativo."];
        }

        return errors;
    }
}
