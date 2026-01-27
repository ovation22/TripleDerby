using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using TripleDerby.Core.Abstractions.Data;
using TripleDerby.Infrastructure.Data;

namespace TripleDerby.Api.Config;

[ExcludeFromCodeCoverage]
public static class DatabaseConfig
{
    public static void AddDatabaseConfig(this IServiceCollection services, IConfiguration configuration)
    {
        var conn = configuration.GetConnectionString("TripleDerby");
        services.AddDbContextPool<TripleDerbyContext>(options =>
            options.UseSqlServer(conn, b => b.MigrationsAssembly("TripleDerby.Infrastructure")));

        services.AddScoped<DbContext>(sp => sp.GetRequiredService<TripleDerbyContext>());
        services.AddScoped<ITransactionManager, TransactionManager>();
    }
}