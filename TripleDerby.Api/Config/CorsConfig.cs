using System.Diagnostics.CodeAnalysis;

namespace TripleDerby.Api.Config;

[ExcludeFromCodeCoverage]
public static class CorsConfig
{
    public static void AddCorsConfig(this IServiceCollection services)
    {
        services.AddCors(options => options.AddPolicy("AllowAll",
            p => p.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
                .WithExposedHeaders("traceparent", "tracestate", "X-Trace-Id", "X-Correlation-ID")));
    }

    public static void UseCorsConfig(this IApplicationBuilder app)
    {
        app.UseCors("AllowAll");
    }
}