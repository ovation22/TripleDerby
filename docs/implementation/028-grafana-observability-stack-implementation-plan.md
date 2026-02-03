# Grafana Observability Stack - Implementation Plan

## Overview

**Feature**: [Feature 028: Grafana Observability Stack](../features/028-grafana-observability-stack.md)
**Approach**: Infrastructure as Code with Aspire Container Resources
**Total Phases**: 5

## Summary

This implementation adds a complete observability stack (Grafana, Loki, Prometheus, OpenTelemetry Collector) to the TripleDerby Aspire AppHost. Unlike typical TDD feature development, this is infrastructure-focused work where "tests" are manual validation that containers start correctly and telemetry flows through the pipeline. Each phase builds incrementally, starting with the simplest container (Prometheus) and progressing to the full integrated stack. The approach emphasizes quick validation loops to catch configuration issues early.

---

## Phase 1: Foundation - Prometheus & OTLP Collector

**Goal**: Establish the metrics pipeline with Prometheus and OpenTelemetry Collector.

**Vertical Slice**: Services can emit metrics to OTLP Collector, which exports to Prometheus for storage.

### Tasks

#### 1. Create Prometheus Configuration

**File to Create**: `TripleDerby.AppHost/prometheus.yml`

```yaml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: 'otel-collector'
    static_configs:
      - targets: ['otel-collector:8889']
```

**Purpose**: Configure Prometheus to scrape metrics from OpenTelemetry Collector's Prometheus exporter endpoint.

---

#### 2. Create OpenTelemetry Collector Configuration

**File to Create**: `TripleDerby.AppHost/otel-collector-config.yaml`

```yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317
      http:
        endpoint: 0.0.0.0:4318

processors:
  batch:
    timeout: 10s

exporters:
  prometheus:
    endpoint: "0.0.0.0:8889"
    namespace: "triple_derby"
    const_labels:
      app: "triple-derby"

  debug:
    verbosity: detailed

service:
  pipelines:
    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [prometheus, debug]
```

**Purpose**: Configure OTLP Collector to receive metrics via OTLP protocol and export to Prometheus. Debug exporter helps validate telemetry is flowing.

**Note**: Starting with metrics-only pipeline. Logs and traces added in later phases.

---

#### 3. Add Prometheus Container to AppHost

**File to Modify**: `TripleDerby.AppHost/Program.cs`

**Insert after line 26 (after RabbitMQ setup):**

```csharp
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
```

**Tasks**:
- [ ] Add Prometheus container resource definition
- [ ] Configure bind mount for `prometheus.yml`
- [ ] Set persistent lifetime for data retention
- [ ] Expose port 9090 for Prometheus UI access

---

#### 4. Add OpenTelemetry Collector Container to AppHost

**File to Modify**: `TripleDerby.AppHost/Program.cs`

**Insert after Prometheus container definition:**

```csharp
// OpenTelemetry Collector - Receives OTLP telemetry and routes to backends
var otelCollector = builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib", "0.115.1")
    .WithHttpEndpoint(port: 4317, targetPort: 4317, name: "otlp-grpc")
    .WithHttpEndpoint(port: 4318, targetPort: 4318, name: "otlp-http")
    .WithHttpEndpoint(port: 8889, targetPort: 8889, name: "prometheus-exporter")
    .WithBindMount("./otel-collector-config.yaml", "/etc/otelcol-contrib/config.yaml")
    .WithReference(prometheus)
    .WithLifetime(ContainerLifetime.Persistent);
```

**Tasks**:
- [ ] Add OTLP Collector container resource definition
- [ ] Configure three endpoints: OTLP gRPC (4317), OTLP HTTP (4318), Prometheus exporter (8889)
- [ ] Configure bind mount for `otel-collector-config.yaml`
- [ ] Add reference to Prometheus for internal networking
- [ ] Set persistent lifetime

---

#### 5. Update API Service to Use OTLP Collector

**File to Modify**: `TripleDerby.AppHost/Program.cs`

**Modify the apiService definition (line 28-34):**

```csharp
var apiService = builder.AddProject<Projects.TripleDerby_Api>("api")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(sql)
    .WaitFor(sql)
    .WithReference(rabbit)
    .WaitFor(rabbit)
    .WithReference(otelCollector)
    .WaitFor(otelCollector)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://otel-collector:4317");
```

**Tasks**:
- [ ] Add `.WithReference(otelCollector)` to wire service discovery
- [ ] Add `.WaitFor(otelCollector)` to ensure collector starts first
- [ ] Add environment variable `OTEL_EXPORTER_OTLP_ENDPOINT` pointing to collector

---

### Validation Steps

#### Manual Testing Checklist:

- [ ] **Start AppHost**: Run `dotnet run` from `TripleDerby.AppHost` directory
- [ ] **Verify Prometheus starts**: Check Aspire dashboard shows Prometheus container running
- [ ] **Verify OTLP Collector starts**: Check Aspire dashboard shows otel-collector container running
- [ ] **Access Prometheus UI**: Navigate to http://localhost:9090
- [ ] **Verify scrape target**: In Prometheus UI, go to Status → Targets, verify `otel-collector` target is UP
- [ ] **Check metrics exist**: In Prometheus UI, query `up{job="otel-collector"}` should return 1
- [ ] **Verify API metrics**: Query `http_server_request_duration_seconds_count` (ASP.NET Core metric) should show data after making API requests
- [ ] **Test API endpoint**: Make a request to the API (e.g., GET horses), verify request succeeds
- [ ] **Verify collector logs**: Check Aspire dashboard logs for otel-collector, should see "Everything is ready" message

**Expected Outcome**: Prometheus UI shows metrics from the API service flowing through OTLP Collector.

---

### Acceptance Criteria

- [ ] Prometheus container starts successfully
- [ ] OTLP Collector container starts successfully
- [ ] API service connects to OTLP Collector
- [ ] Prometheus scrapes metrics from OTLP Collector successfully
- [ ] ASP.NET Core metrics (e.g., `http_server_request_duration_seconds`) visible in Prometheus UI
- [ ] No errors in Aspire dashboard logs for prometheus or otel-collector containers

**Deliverable**: Working metrics pipeline from API → OTLP Collector → Prometheus.

---

## Phase 2: Add Loki for Log Aggregation

**Goal**: Extend the telemetry pipeline to include structured log aggregation with Loki.

**Vertical Slice**: Services emit structured logs to OTLP Collector, which exports to Loki for storage and querying.

### Tasks

#### 1. Add Loki Container to AppHost

**File to Modify**: `TripleDerby.AppHost/Program.cs`

**Insert before Prometheus container definition:**

```csharp
// Loki - Log aggregation
var loki = builder.AddContainer("loki", "grafana/loki", "3.3.2")
    .WithHttpEndpoint(port: 3100, targetPort: 3100, name: "http")
    .WithArgs("--config.file=/etc/loki/local-config.yaml")
    .WithLifetime(ContainerLifetime.Persistent);
```

**Tasks**:
- [ ] Add Loki container resource definition
- [ ] Expose port 3100 for Loki API
- [ ] Use default embedded config (`local-config.yaml` included in image)
- [ ] Set persistent lifetime

---

#### 2. Update OTLP Collector Configuration for Logs

**File to Modify**: `TripleDerby.AppHost/otel-collector-config.yaml`

**Add Loki exporter to exporters section:**

```yaml
exporters:
  prometheus:
    endpoint: "0.0.0.0:8889"
    namespace: "triple_derby"
    const_labels:
      app: "triple-derby"

  loki:
    endpoint: "http://loki:3100/loki/api/v1/push"
    labels:
      resource:
        service.name: "service_name"
      attributes:
        level: "severity"

  debug:
    verbosity: detailed
```

**Add logs pipeline to service.pipelines section:**

```yaml
service:
  pipelines:
    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [prometheus, debug]

    logs:
      receivers: [otlp]
      processors: [batch]
      exporters: [loki, debug]
```

**Tasks**:
- [ ] Add Loki exporter with correct endpoint URL
- [ ] Configure log labels to include service name and severity level
- [ ] Add logs pipeline routing OTLP logs to Loki
- [ ] Keep debug exporter to validate logs are flowing

---

#### 3. Update OTLP Collector to Reference Loki

**File to Modify**: `TripleDerby.AppHost/Program.cs`

**Modify otelCollector definition to add Loki dependency:**

```csharp
var otelCollector = builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib", "0.115.1")
    .WithHttpEndpoint(port: 4317, targetPort: 4317, name: "otlp-grpc")
    .WithHttpEndpoint(port: 4318, targetPort: 4318, name: "otlp-http")
    .WithHttpEndpoint(port: 8889, targetPort: 8889, name: "prometheus-exporter")
    .WithBindMount("./otel-collector-config.yaml", "/etc/otelcol-contrib/config.yaml")
    .WithReference(loki)
    .WaitFor(loki)
    .WithReference(prometheus)
    .WithLifetime(ContainerLifetime.Persistent);
```

**Tasks**:
- [ ] Add `.WithReference(loki)` to enable internal networking
- [ ] Add `.WaitFor(loki)` to ensure Loki starts before collector

---

### Validation Steps

#### Manual Testing Checklist:

- [ ] **Restart AppHost**: Stop and restart to pick up new configuration
- [ ] **Verify Loki starts**: Check Aspire dashboard shows Loki container running
- [ ] **Verify OTLP Collector restarts**: Should restart with new config including Loki exporter
- [ ] **Check Loki ready endpoint**: Navigate to http://localhost:3100/ready should return "ready"
- [ ] **Query Loki logs via API**:
  ```bash
  curl -G -s "http://localhost:3100/loki/api/v1/query" --data-urlencode 'query={service_name="api"}'
  ```
  Should return JSON with log entries from API service
- [ ] **Trigger log messages**: Make API requests to generate logs, verify they appear in Loki
- [ ] **Check collector logs**: Verify no errors in otel-collector logs about Loki exporter

**Expected Outcome**: Structured logs from API service flow to Loki and are queryable via Loki API.

---

### Acceptance Criteria

- [ ] Loki container starts successfully
- [ ] OTLP Collector restarts with logs pipeline enabled
- [ ] API service logs appear in Loki within 15 seconds of being emitted
- [ ] Logs are queryable via Loki API by service name
- [ ] Log entries include severity level (Info, Warning, Error)
- [ ] No errors in Aspire dashboard logs for loki or otel-collector containers

**Deliverable**: Working logs pipeline from API → OTLP Collector → Loki.

---

## Phase 3: Add Grafana with Dashboards

**Goal**: Deploy Grafana with pre-configured data sources and .NET dashboards 19924/19925.

**Vertical Slice**: Developers can access Grafana UI and view ASP.NET Core metrics in dashboard 19924.

### Tasks

#### 1. Create Grafana Provisioning Directory Structure

**Directories to Create**:
- `TripleDerby.AppHost/grafana/provisioning/datasources/`
- `TripleDerby.AppHost/grafana/provisioning/dashboards/`
- `TripleDerby.AppHost/grafana/dashboards/`

**Tasks**:
- [ ] Create `grafana/provisioning/datasources/` directory
- [ ] Create `grafana/provisioning/dashboards/` directory
- [ ] Create `grafana/dashboards/` directory for dashboard JSON files

---

#### 2. Create Grafana Data Sources Configuration

**File to Create**: `TripleDerby.AppHost/grafana/provisioning/datasources/datasources.yaml`

```yaml
apiVersion: 1

datasources:
  - name: Prometheus
    type: prometheus
    access: proxy
    url: http://prometheus:9090
    isDefault: true
    editable: false
    jsonData:
      timeInterval: "15s"

  - name: Loki
    type: loki
    access: proxy
    url: http://loki:3100
    editable: false
    jsonData:
      maxLines: 1000
```

**Tasks**:
- [ ] Define Prometheus data source pointing to prometheus:9090
- [ ] Define Loki data source pointing to loki:3100
- [ ] Set Prometheus as default data source
- [ ] Make data sources non-editable to prevent accidental changes

---

#### 3. Create Grafana Dashboard Provisioning Configuration

**File to Create**: `TripleDerby.AppHost/grafana/provisioning/dashboards/dashboards.yaml`

```yaml
apiVersion: 1

providers:
  - name: 'TripleDerby Dashboards'
    orgId: 1
    folder: 'ASP.NET Core'
    type: file
    disableDeletion: false
    updateIntervalSeconds: 10
    allowUiUpdates: true
    options:
      path: /var/lib/grafana/dashboards
      foldersFromFilesStructure: false
```

**Tasks**:
- [ ] Configure dashboard provider to load from mounted directory
- [ ] Place dashboards in "ASP.NET Core" folder
- [ ] Allow UI updates for dashboard customization during development

---

#### 4. Download ASP.NET Core Dashboard 19924

**File to Create**: `TripleDerby.AppHost/grafana/dashboards/aspnetcore-19924.json`

**Manual Steps**:
1. Navigate to https://grafana.com/grafana/dashboards/19924-asp-net-core/
2. Click "Download JSON" button
3. Save to `TripleDerby.AppHost/grafana/dashboards/aspnetcore-19924.json`

**Post-Download Edit** (if needed):
- Verify datasource UID matches "Prometheus" (our provisioned name)
- Update any hardcoded datasource references to `"datasource": "Prometheus"`

**Tasks**:
- [ ] Download dashboard 19924 JSON from Grafana.com
- [ ] Save to `grafana/dashboards/aspnetcore-19924.json`
- [ ] Verify datasource references are correct

---

#### 5. Download ASP.NET Core Endpoint Dashboard 19925

**File to Create**: `TripleDerby.AppHost/grafana/dashboards/aspnetcore-endpoint-19925.json`

**Manual Steps**:
1. Navigate to https://grafana.com/grafana/dashboards/19925-asp-net-core-endpoint/
2. Click "Download JSON" button
3. Save to `TripleDerby.AppHost/grafana/dashboards/aspnetcore-endpoint-19925.json`

**Tasks**:
- [ ] Download dashboard 19925 JSON from Grafana.com
- [ ] Save to `grafana/dashboards/aspnetcore-endpoint-19925.json`
- [ ] Verify datasource references are correct

---

#### 6. Add Grafana Container to AppHost

**File to Modify**: `TripleDerby.AppHost/Program.cs`

**Insert after OTLP Collector container definition:**

```csharp
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
    .WithReference(prometheus)
    .WaitFor(prometheus)
    .WithReference(loki)
    .WaitFor(loki)
    .WithLifetime(ContainerLifetime.Persistent);
```

**Tasks**:
- [ ] Add Grafana container resource definition
- [ ] Configure bind mounts for provisioning and dashboards
- [ ] Enable anonymous access for local development convenience
- [ ] Set admin credentials (admin/admin)
- [ ] Reference Prometheus and Loki for container networking
- [ ] Expose port 3000 for Grafana UI

---

### Validation Steps

#### Manual Testing Checklist:

- [ ] **Restart AppHost**: Stop and restart to deploy Grafana
- [ ] **Verify Grafana starts**: Check Aspire dashboard shows Grafana container running
- [ ] **Access Grafana UI**: Navigate to http://localhost:3000
- [ ] **Verify anonymous access**: Should land directly on Home page without login prompt
- [ ] **Check data sources**: Go to Configuration → Data Sources
  - [ ] Prometheus data source exists and is default
  - [ ] Loki data source exists
  - [ ] Test both data sources (should show green "Data source is working")
- [ ] **Check dashboards loaded**: Go to Dashboards → Browse
  - [ ] "ASP.NET Core" folder exists
  - [ ] Dashboard 19924 (ASP.NET Core) is present
  - [ ] Dashboard 19925 (ASP.NET Core Endpoint) is present
- [ ] **Open dashboard 19924**: Click to open ASP.NET Core dashboard
  - [ ] Dashboard loads without errors
  - [ ] Panels show data (may need to wait 15-30 seconds for first scrape)
  - [ ] Select "api" service in service dropdown
  - [ ] Verify request rate, duration, error rate panels show data
- [ ] **Test Loki in Explore**: Go to Explore tab
  - [ ] Select Loki data source
  - [ ] Query: `{service_name="api"}`
  - [ ] Verify log lines appear

**Expected Outcome**: Grafana UI accessible with pre-configured dashboards showing live ASP.NET Core metrics.

---

### Acceptance Criteria

- [ ] Grafana container starts successfully
- [ ] Anonymous access enabled (no login required)
- [ ] Prometheus and Loki data sources auto-configured and working
- [ ] Dashboard 19924 loads and displays ASP.NET Core metrics
- [ ] Dashboard 19925 loads and displays endpoint-level metrics
- [ ] Metrics update in near real-time (< 30 second delay)
- [ ] Logs queryable through Grafana Explore interface
- [ ] No errors in Grafana container logs

**Deliverable**: Fully functional Grafana UI with .NET dashboards visualizing live telemetry.

---

## Phase 4: Extend to All Microservices

**Goal**: Connect all 6 microservices to the observability stack.

**Vertical Slice**: Metrics and logs from all services (API, Web, Breeding, Feeding, Training, Racing) flow through the observability pipeline.

### Tasks

#### 1. Update Web Service

**File to Modify**: `TripleDerby.AppHost/Program.cs`

**Modify Web service definition (currently around line 36-38):**

```csharp
builder.AddProject<Projects.TripleDerby_Web>("admin")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithReference(otelCollector)
    .WaitFor(otelCollector)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://otel-collector:4317");
```

**Tasks**:
- [ ] Add `.WithReference(otelCollector)`
- [ ] Add `.WaitFor(otelCollector)`
- [ ] Add `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable

---

#### 2. Update Breeding Service

**File to Modify**: `TripleDerby.AppHost/Program.cs`

**Modify Breeding service definition:**

```csharp
builder.AddProject<Projects.TripleDerby_Services_Breeding>("breeding")
    .WithReference(sql)
    .WaitFor(sql)
    .WithReference(rabbit)
    .WaitFor(rabbit)
    .WithReference(otelCollector)
    .WaitFor(otelCollector)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://otel-collector:4317");
```

**Tasks**:
- [ ] Add `.WithReference(otelCollector)`
- [ ] Add `.WaitFor(otelCollector)`
- [ ] Add `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable

---

#### 3. Update Feeding Service

**File to Modify**: `TripleDerby.AppHost/Program.cs`

**Modify Feeding service definition:**

```csharp
builder.AddProject<Projects.TripleDerby_Services_Feeding>("feeding")
    .WithReference(sql)
    .WaitFor(sql)
    .WithReference(rabbit)
    .WaitFor(rabbit)
    .WithReference(otelCollector)
    .WaitFor(otelCollector)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://otel-collector:4317");
```

**Tasks**:
- [ ] Add `.WithReference(otelCollector)`
- [ ] Add `.WaitFor(otelCollector)`
- [ ] Add `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable

---

#### 4. Update Training Service

**File to Modify**: `TripleDerby.AppHost/Program.cs`

**Modify Training service definition:**

```csharp
builder.AddProject<Projects.TripleDerby_Services_Training>("training")
    .WithReference(sql)
    .WaitFor(sql)
    .WithReference(rabbit)
    .WaitFor(rabbit)
    .WithReference(otelCollector)
    .WaitFor(otelCollector)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://otel-collector:4317");
```

**Tasks**:
- [ ] Add `.WithReference(otelCollector)`
- [ ] Add `.WaitFor(otelCollector)`
- [ ] Add `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable

---

#### 5. Update Racing Service

**File to Modify**: `TripleDerby.AppHost/Program.cs`

**Modify Racing service definition:**

```csharp
builder.AddProject<Projects.TripleDerby_Services_Racing>("racing")
    .WithReference(sql)
    .WaitFor(sql)
    .WithReference(rabbit)
    .WaitFor(rabbit)
    .WithReference(otelCollector)
    .WaitFor(otelCollector)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://otel-collector:4317");
```

**Tasks**:
- [ ] Add `.WithReference(otelCollector)`
- [ ] Add `.WaitFor(otelCollector)`
- [ ] Add `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable

---

### Validation Steps

#### Manual Testing Checklist:

- [ ] **Restart AppHost**: Stop and restart all services
- [ ] **Verify all services start**: Check Aspire dashboard shows all 6 services running
- [ ] **Check Grafana service dropdown**: In dashboard 19924, service dropdown should show all services:
  - [ ] api
  - [ ] admin (Web)
  - [ ] breeding
  - [ ] feeding
  - [ ] training
  - [ ] racing
- [ ] **Verify metrics per service**: Select each service in dropdown, verify panels populate with data
- [ ] **Verify logs per service**: In Grafana Explore, query logs for each service:
  - `{service_name="api"}`
  - `{service_name="admin"}`
  - `{service_name="breeding"}`
  - `{service_name="feeding"}`
  - `{service_name="training"}`
  - `{service_name="racing"}`
- [ ] **Test cross-service workflow**: Trigger a race run (involves multiple services), verify metrics/logs from all involved services

**Expected Outcome**: All 6 microservices emit telemetry visible in Grafana dashboards and Loki logs.

---

### Acceptance Criteria

- [ ] All 6 services connected to OTLP Collector
- [ ] Metrics from all services visible in Prometheus
- [ ] Dashboard 19924 service dropdown lists all 6 services
- [ ] Logs from all services queryable in Loki
- [ ] No errors in service startup due to OTLP configuration
- [ ] Cross-service workflows generate correlated telemetry

**Deliverable**: Complete observability coverage across all microservices.

---

## Phase 5: Documentation & Optimization

**Goal**: Document the observability stack and optimize for best developer experience.

**Vertical Slice**: New developers can start the stack and understand how to use Grafana/Prometheus/Loki for debugging.

### Tasks

#### 1. Create Observability Documentation

**File to Create**: `docs/OBSERVABILITY.md`

**Content Outline**:
- Overview of observability stack components
- How to access each component (URLs)
- Quick start guide for common tasks:
  - Viewing metrics in Grafana dashboard 19924
  - Querying logs in Loki
  - Using Prometheus for ad-hoc queries
  - Correlating logs and traces
- Troubleshooting guide:
  - Container won't start
  - No metrics appearing
  - Logs not showing up
  - Dashboard shows "No data"
- Advanced topics:
  - Creating custom dashboards
  - Adding custom metrics
  - Configuring alerts (future feature)

**Tasks**:
- [ ] Write observability overview section
- [ ] Document access URLs and credentials
- [ ] Create quick start guide with screenshots
- [ ] Write troubleshooting section
- [ ] Document common LogQL queries for Loki

---

#### 2. Add README.md in AppHost/grafana/

**File to Create**: `TripleDerby.AppHost/grafana/README.md`

**Content**:
```markdown
# Grafana Configuration

This directory contains Grafana provisioning configuration and dashboards.

## Structure

- `provisioning/datasources/` - Auto-configured data sources (Prometheus, Loki)
- `provisioning/dashboards/` - Dashboard provider configuration
- `dashboards/` - Dashboard JSON files

## Dashboards

### ASP.NET Core (19924)
Official .NET dashboard showing application-level metrics:
- Request rate, duration, errors
- HTTP server metrics
- ASP.NET Core runtime metrics

Source: https://grafana.com/grafana/dashboards/19924-asp-net-core/

### ASP.NET Core Endpoint (19925)
Endpoint-level performance metrics:
- Per-endpoint request rates
- Latency distributions
- Error rates by endpoint

Source: https://grafana.com/grafana/dashboards/19925-asp-net-core-endpoint/

## Updating Dashboards

To update dashboards to newer versions:
1. Visit the dashboard page on grafana.com
2. Download the latest JSON
3. Replace the existing JSON file
4. Restart Grafana container: `docker restart grafana`

## Customizing Dashboards

Dashboards can be edited in the Grafana UI. Changes are allowed (`allowUiUpdates: true`) but not persisted. To persist changes:
1. Edit dashboard in Grafana UI
2. Export JSON (Share → Export)
3. Save to `dashboards/` directory
4. Restart Grafana to reload
```

**Tasks**:
- [ ] Create README explaining Grafana directory structure
- [ ] Document dashboard sources and update process
- [ ] Explain customization workflow

---

#### 3. Update Main Project README

**File to Modify**: `README.md` (root)

**Add Observability section** (after Architecture or Development Setup section):

```markdown
## Observability

TripleDerby uses Grafana, Prometheus, and Loki for observability:

- **Grafana**: http://localhost:3000 (no login required)
- **Prometheus**: http://localhost:9090
- **Loki**: http://localhost:3100

### Quick Start

1. Start the AppHost: `dotnet run --project TripleDerby.AppHost`
2. Wait for all containers to start (~30 seconds)
3. Open Grafana: http://localhost:3000
4. Navigate to Dashboards → ASP.NET Core
5. Select a service from the dropdown to view metrics

See [docs/OBSERVABILITY.md](docs/OBSERVABILITY.md) for detailed guide.
```

**Tasks**:
- [ ] Add Observability section to main README
- [ ] Include access URLs for all components
- [ ] Link to detailed observability documentation

---

#### 4. Optimize OTLP Collector Configuration

**File to Modify**: `TripleDerby.AppHost/otel-collector-config.yaml`

**Remove debug exporter** (no longer needed for production):

```yaml
exporters:
  prometheus:
    endpoint: "0.0.0.0:8889"
    namespace: "triple_derby"
    const_labels:
      app: "triple-derby"

  loki:
    endpoint: "http://loki:3100/loki/api/v1/push"
    labels:
      resource:
        service.name: "service_name"
      attributes:
        level: "severity"

service:
  pipelines:
    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [prometheus]

    logs:
      receivers: [otlp]
      processors: [batch]
      exporters: [loki]
```

**Tasks**:
- [ ] Remove debug exporter from exporters section
- [ ] Remove debug exporter from all pipelines
- [ ] Clean up configuration formatting

---

#### 5. Add .gitignore Entries for Grafana Data

**File to Modify**: `.gitignore` (root)

**Add at end of file:**

```gitignore
# Grafana runtime data (if using volume mounts instead of bind mounts)
TripleDerby.AppHost/grafana/data/
TripleDerby.AppHost/grafana/plugins/
TripleDerby.AppHost/grafana/logs/
```

**Tasks**:
- [ ] Add gitignore entries for Grafana runtime directories
- [ ] Ensure dashboard JSON files ARE committed (not ignored)

---

### Validation Steps

#### Documentation Review:

- [ ] **Read through OBSERVABILITY.md**: Ensure it's clear and actionable
- [ ] **Follow quick start guide**: Verify instructions work as written
- [ ] **Test troubleshooting steps**: Simulate common issues and verify fixes work
- [ ] **Check all links**: Ensure URLs and file paths are correct

#### Developer Experience Test:

- [ ] **Clean slate test**:
  1. Stop AppHost
  2. Clear all containers: `docker container prune -f`
  3. Start AppHost from scratch
  4. Time how long until Grafana dashboard shows data
  5. Should be < 60 seconds from start to data visible
- [ ] **New developer simulation**:
  - Read only the README observability section
  - Attempt to view metrics without referring to detailed docs
  - Should be successful

**Expected Outcome**: Complete, clear documentation enabling any developer to use the observability stack.

---

### Acceptance Criteria

- [ ] `docs/OBSERVABILITY.md` created with comprehensive guide
- [ ] Main README updated with observability section
- [ ] Grafana directory README explains configuration
- [ ] All documentation links and URLs verified working
- [ ] OTLP Collector configuration optimized (debug exporter removed)
- [ ] Gitignore prevents committing Grafana runtime data
- [ ] Cold start to first metrics < 60 seconds
- [ ] New developer can use stack with minimal guidance

**Deliverable**: Production-ready observability stack with complete documentation.

---

## Files Summary

### New Files Created

**Configuration Files:**
- `TripleDerby.AppHost/prometheus.yml` - Prometheus scrape configuration
- `TripleDerby.AppHost/otel-collector-config.yaml` - OpenTelemetry Collector pipelines
- `TripleDerby.AppHost/grafana/provisioning/datasources/datasources.yaml` - Grafana data sources
- `TripleDerby.AppHost/grafana/provisioning/dashboards/dashboards.yaml` - Dashboard provider config
- `TripleDerby.AppHost/grafana/dashboards/aspnetcore-19924.json` - ASP.NET Core dashboard
- `TripleDerby.AppHost/grafana/dashboards/aspnetcore-endpoint-19925.json` - Endpoint dashboard

**Documentation:**
- `docs/OBSERVABILITY.md` - Complete observability guide
- `TripleDerby.AppHost/grafana/README.md` - Grafana configuration guide
- `docs/implementation/028-grafana-observability-stack-implementation-plan.md` - This file

### Modified Files

- `TripleDerby.AppHost/Program.cs` - Added container resources and service references
- `README.md` - Added observability section
- `.gitignore` - Added Grafana runtime data entries

---

## Milestones

| Milestone | After Phase | What's Working |
|-----------|-------------|----------------|
| Metrics Pipeline | Phase 1 | API metrics flowing to Prometheus |
| Logs Pipeline | Phase 2 | API logs aggregated in Loki |
| Grafana Dashboards | Phase 3 | .NET dashboards visualizing telemetry |
| Full Coverage | Phase 4 | All 6 services observable |
| Production Ready | Phase 5 | Documented, optimized, ready for daily use |

---

## Risks & Mitigation

| Risk | Mitigation | Phase |
|------|------------|-------|
| Port conflicts with existing services | Use standard ports; document alternatives | 1 |
| Container startup ordering issues | Use `.WaitFor()` and `.WithReference()` | 1 |
| High memory usage (~2GB additional) | Monitor resource usage; use Persistent lifetime | 1 |
| Dashboard JSON format changes | Document source URLs; test after updates | 3 |
| Complex LogQL queries for developers | Provide common query examples in docs | 5 |
| OTLP Collector misconfiguration | Start with simple config; add complexity incrementally | 1-2 |

---

## Success Criteria

### Technical Success
- [ ] All 5 phases implemented without errors
- [ ] All 4 observability containers running stably
- [ ] All 6 microservices emitting telemetry
- [ ] Dashboard 19924 showing live metrics from all services
- [ ] Logs from all services queryable in Loki
- [ ] Container startup time < 30 seconds
- [ ] No regressions in existing Aspire functionality

### Developer Experience Success
- [ ] Single command starts entire stack
- [ ] Grafana accessible without login
- [ ] Dashboards pre-loaded and working
- [ ] Documentation clear and actionable
- [ ] New developers productive with observability stack in < 15 minutes

### Operational Success
- [ ] Configuration stored in source control
- [ ] No manual setup required after git clone
- [ ] Data persists across AppHost restarts
- [ ] Clear troubleshooting guidance available

---

## Implementation Notes

### Why This Approach?

**Infrastructure-First**: Unlike typical features, observability is infrastructure. We build and validate incrementally rather than test-first.

**Quick Validation Loops**: Each phase includes manual validation steps to catch configuration errors early. Container orchestration fails fast, so we verify at each step.

**Incremental Complexity**: Start with metrics (simplest), add logs (medium), then visualization (complex), finally scale to all services.

**Documentation-Heavy**: Observability tools are useless if developers don't know how to use them. Comprehensive docs are part of the deliverable.

### Common Pitfalls

1. **Bind mount paths**: Use `./relative/path`, not absolute paths. Aspire resolves relative to AppHost project directory.

2. **Container networking**: Use container names (e.g., `prometheus:9090`), not `localhost:9090` for inter-container communication.

3. **OTLP endpoint**: Must be `http://otel-collector:4317` for services, not `http://localhost:4317`.

4. **Dashboard data sources**: Grafana dashboard JSON must reference provisioned data source names exactly.

5. **Wait dependencies**: Always `.WaitFor()` dependent containers to ensure correct startup order.

---

## Next Steps After Implementation

Once all phases complete:

1. **Commit Changes**: Commit the working observability stack to the feature branch
2. **Test Clean Environment**: Another developer should clone and verify it works
3. **Create PR**: Open pull request with link to this implementation plan
4. **Team Demo**: Show the team how to use Grafana/Prometheus/Loki

**Future Enhancements** (separate features):
- Add Tempo for distributed tracing (alternative to Aspire dashboard)
- Configure Grafana alerts for critical metrics
- Add custom TripleDerby business metric dashboards
- Integrate with Aspire dashboard for unified view
- Add Jaeger or Zipkin for advanced trace analysis
