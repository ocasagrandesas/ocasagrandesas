using OCasaGrande.Web.Domain;

namespace OCasaGrande.Web.Data;

public static class ModuleSeed
{
    private static readonly DateTimeOffset SeedDate = new(2026, 10, 28, 0, 0, 0, TimeSpan.Zero);

    public static PlatformModule[] Modules =>
    [
        new()
        {
            Id = 1,
            Key = "monitoramento",
            Name = "Monitoramento do negócio",
            Description = "Acompanhamento dos principais indicadores do negócio em um só painel.",
            IsEnabled = true,
            UpdatedAt = SeedDate,
        },
    ];
}
