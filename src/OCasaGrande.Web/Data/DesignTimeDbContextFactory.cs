using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OCasaGrande.Web.Data;

// Used only by `dotnet ef migrations add`, which builds the model without connecting to a database.
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>();
        AppDbContext.Configure(options, "Host=localhost;Database=ocasagrande");
        return new AppDbContext(options.Options);
    }
}
