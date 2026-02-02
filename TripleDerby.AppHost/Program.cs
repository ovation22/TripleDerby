var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

// ============================================================================
// DATABASE PROVIDER CONFIGURATION
// See docs/DATABASE_SWITCHING.md for switching between SQL Server and PostgreSQL
// ============================================================================

// SQL SERVER (Commented for local dev)
// var sql = builder.AddSqlServer("sql", port: 59944)
//     .WithDataVolume()
//     .WithLifetime(ContainerLifetime.Persistent)
//     .AddDatabase("TripleDerby");

// POSTGRESQL (Active for local dev)
var postgres = builder.AddPostgres("sql", port: 55432)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin();
var sql = postgres.AddDatabase("TripleDerby");

var rabbit = builder.AddRabbitMQ("messaging")
    .WithDataVolume(isReadOnly: false)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithManagementPlugin();

// ============================================================================
// OBSERVABILITY STACK
// Grafana, Prometheus, Loki, OpenTelemetry Collector for metrics and logs
// ============================================================================

// Prometheus - Metrics storage and querying
var prometheus = builder.AddContainer("prometheus", "prom/prometheus", "v3.1.0")
    .WithHttpEndpoint(port: 9090, targetPort: 9090, name: "http")
    .WithBindMount("./prometheus.yml", "/etc/prometheus/prometheus.yml")
    .WithArgs("--config.file=/etc/prometheus/prometheus.yml",
              "--storage.tsdb.path=/prometheus",
              "--web.console.libraries=/usr/share/prometheus/console_libraries",
              "--web.console.templates=/usr/share/prometheus/consoles")
    .WithLifetime(ContainerLifetime.Persistent);

// OpenTelemetry Collector - Receives OTLP telemetry and routes to backends
var otelCollector = builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib", "0.115.1")
    .WithHttpEndpoint(port: 4317, targetPort: 4317, name: "otlp-grpc")
    .WithHttpEndpoint(port: 4318, targetPort: 4318, name: "otlp-http")
    .WithHttpEndpoint(port: 8889, targetPort: 8889, name: "prometheus-exporter")
    .WithBindMount("./otel-collector-config.yaml", "/etc/otelcol-contrib/config.yaml")
    .WithLifetime(ContainerLifetime.Persistent);

// Grafana - Metrics and logs visualization
var grafana = builder.AddContainer("grafana", "grafana/grafana", "11.4.0")
    .WithHttpEndpoint(port: 3000, targetPort: 3000, name: "http")
    .WithBindMount("./grafana/provisioning", "/etc/grafana/provisioning")
    .WithBindMount("./grafana/dashboards", "/var/lib/grafana/dashboards")
    .WithEnvironment("GF_AUTH_ANONYMOUS_ENABLED", "true")
    .WithEnvironment("GF_AUTH_ANONYMOUS_ORG_ROLE", "Admin")
    .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", "admin")
    .WithEnvironment("GF_SECURITY_ADMIN_USER", "admin")
    .WithEnvironment("GF_USERS_ALLOW_SIGN_UP", "false")
    .WithLifetime(ContainerLifetime.Persistent);

var apiService = builder.AddProject<Projects.TripleDerby_Api>("api")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(sql)
    .WaitFor(sql)
    .WithReference(rabbit)
    .WaitFor(rabbit)
    .WaitFor(otelCollector)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317");

builder.AddProject<Projects.TripleDerby_Web>("admin")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WaitFor(otelCollector)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317");

builder.AddProject<Projects.TripleDerby_Services_Breeding>("breeding")
    .WithReference(sql)
    .WaitFor(sql)
    .WithReference(rabbit)
    .WaitFor(rabbit)
    .WaitFor(otelCollector)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317");

builder.AddProject<Projects.TripleDerby_Services_Feeding>("feeding")
    .WithReference(sql)
    .WaitFor(sql)
    .WithReference(rabbit)
    .WaitFor(rabbit)
    .WaitFor(otelCollector)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317");

builder.AddProject<Projects.TripleDerby_Services_Training>("training")
    .WithReference(sql)
    .WaitFor(sql)
    .WithReference(rabbit)
    .WaitFor(rabbit)
    .WaitFor(otelCollector)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317");

builder.AddProject<Projects.TripleDerby_Services_Racing>("racing")
    .WithReference(sql)
    .WaitFor(sql)
    .WithReference(rabbit)
    .WaitFor(rabbit)
    .WaitFor(otelCollector)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317");

builder.Build().Run();
