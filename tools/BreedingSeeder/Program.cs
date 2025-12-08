using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

//
// BreedingSeeder — optimized for throughput
// - bounded concurrency via fixed worker tasks (no Task-per-request)
// - tuned SocketsHttpHandler (connection pooling, HTTP/2, no cookies/redirects)
// - pre-serializes payloads per parent pair (no JSON in hot path)
// - sends with HttpCompletionOption.ResponseHeadersRead and does NOT read bodies
// - minimal synchronous logging: only a few sampled error messages are kept
//

IConfigurationRoot config = new ConfigurationBuilder()
    .AddCommandLine(args)
    .AddEnvironmentVariables()
    .Build();

Dictionary<string, string> cmdArgs = ParseArgs(args);

// Required/simple args 
string apiBase = cmdArgs.GetValueOrDefault("apiBase") ?? PromptString("API base URL", "https://localhost:7523");

Guid ownerId = cmdArgs.TryGetValue("ownerId", out string? ownerStr) && Guid.TryParse(ownerStr, out Guid parsedOwner)
    ? parsedOwner
    : PromptGuid("OwnerId (GUID)", new Guid("72115894-88CD-433E-9892-CAC22E335F1D"));

long count = cmdArgs.TryGetValue("count", out string? cStr) && long.TryParse(cStr, out long cVal)
    ? cVal
    : PromptLong("Total requests to create", 1_600);

// Concurrency (virtual users)
int concurrency = cmdArgs.TryGetValue("concurrency", out string? concStr) && int.TryParse(concStr, out int concVal)
    ? Math.Max(1, concVal)
    : PromptInt("Total max concurrency", 10);

// Json options for request payloads (lean)
JsonSerializerOptions requestJsonOptions = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

Console.WriteLine();
Console.WriteLine("Configuration:");
Console.WriteLine($"  API Base:    {apiBase}");
Console.WriteLine($"  OwnerId:     {ownerId}");
Console.WriteLine($"  Count:       {count:N0}");
Console.WriteLine($"  Concurrency: {concurrency}");
Console.WriteLine();
Console.Write("Press Enter to start or Ctrl-C to abort...");
Console.ReadLine();

// Tuned handler to reduce connection churn and support concurrency
var handler = new SocketsHttpHandler
{
    MaxConnectionsPerServer = Math.Max(8, concurrency),
    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
    EnableMultipleHttp2Connections = false,
    UseCookies = false,
    AllowAutoRedirect = false
};

using HttpClient http = new(handler)
{
    BaseAddress = new Uri(apiBase),
    DefaultRequestVersion = HttpVersion.Version20,
    DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
    Timeout = TimeSpan.FromSeconds(60) // reasonable per-request timeout
};

try
{
    // Robust parents.json loading (multiple candidate locations and wrappers supported)
    List<ParentPair> parents = LoadParentPairsFromFile("parents.json");
    if (parents.Count == 0)
    {
        Console.Error.WriteLine("parents.json missing or contains no parent pairs. This seeder requires parents.json.");
        Environment.ExitCode = 1;
        return;
    }

    Console.WriteLine($"Loaded parent pairs: {parents.Count}");

    // Pre-serialize payloads per parent pair (no JSON serialization in hot path)
    byte[][] payloads = parents
        .Select(p => JsonSerializer.SerializeToUtf8Bytes(
            new BreedRequestDto(ownerId, p.SireId, p.DamId),
            requestJsonOptions))
        .ToArray();

    Stopwatch sw = Stopwatch.StartNew();

    int failures = 0;
    object sampleLock = new();
    List<string> errorSamples = new(); // keep a few sample error messages

    // Local async sender
    async Task SendAsync(byte[] payload)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "/api/breeding/requests")
            {
                Content = new ByteArrayContent(payload)
            };
            req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            // Return once server responds headers — avoid waiting for body
            using HttpResponseMessage resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead)
                                                       .ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                Interlocked.Increment(ref failures);

                string sample = $"Status {(int)resp.StatusCode} ({resp.StatusCode})";
                lock (sampleLock)
                {
                    if (errorSamples.Count < 10)
                        errorSamples.Add(sample);
                }
            }
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref failures);
            lock (sampleLock)
            {
                if (errorSamples.Count < 10)
                    errorSamples.Add($"Exception: {ex.Message}");
            }
        }
    }

    // Worker pattern: fixed number of workers, each pulls next index via Interlocked
    long nextIndex = 0;

    async Task Worker()
    {
        while (true)
        {
            long i = Interlocked.Increment(ref nextIndex) - 1;
            if (i >= count)
                break;

            byte[] payload = payloads[i % payloads.Length];
            await SendAsync(payload).ConfigureAwait(false);
        }
    }

    // Spin up workers
    Task[] workers = Enumerable.Range(0, concurrency)
                               .Select(_ => Worker())
                               .ToArray();

    // Wait for all in-flight requests to finish.
    await Task.WhenAll(workers).ConfigureAwait(false);

    sw.Stop();

    Console.WriteLine();
    Console.WriteLine("Elapsed: " + sw.Elapsed.ToString(@"hh\:mm\:ss\.fff"));
    Console.WriteLine($"Failures: {failures:N0}");
    
    double seconds = sw.Elapsed.TotalSeconds;
    if (seconds > 0)
    {
        double rps = count / seconds;
        Console.WriteLine($"Throughput: {rps:N0} requests/sec");
    }

    if (errorSamples.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("Sample errors (up to 10):");
        foreach (var s in errorSamples) Console.WriteLine("  " + s);
    }

    Environment.ExitCode = 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Fatal: {ex.Message}");
    Environment.ExitCode = 1;
}

static Dictionary<string, string> ParseArgs(string[] arr)
{
    var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (string a in arr)
    {
        if (a.StartsWith("--"))
        {
            string[] kv = a.Substring(2).Split('=', 2);
            d[kv[0]] = kv.Length == 2 ? kv[1] : "true";
        }
    }
    return d;
}

static string PromptString(string label, string defaultValue)
{
    Console.Write($"{label} [{defaultValue}]: ");
    string? input = Console.ReadLine();
    return string.IsNullOrWhiteSpace(input) ? defaultValue : input.Trim();
}

static long PromptLong(string label, long defaultValue)
{
    Console.Write($"{label} [{defaultValue}]: ");
    string? input = Console.ReadLine();
    return long.TryParse(input, out long v) ? v : defaultValue;
}

static int PromptInt(string label, int defaultValue)
{
    Console.Write($"{label} [{defaultValue}]: ");
    string? input = Console.ReadLine();
    return int.TryParse(input, out int v) ? v : defaultValue;
}

static Guid PromptGuid(string label, Guid defaultValue = default)
{
    while (true)
    {
        if (defaultValue != Guid.Empty) Console.Write($"{label} [{defaultValue}]: ");
        else Console.Write($"{label}: ");
        string? input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
        {
            if (defaultValue != Guid.Empty) return defaultValue;
            Console.WriteLine("A GUID is required.");
            continue;
        }
        if (Guid.TryParse(input.Trim(), out Guid g)) return g;
        Console.WriteLine("Invalid GUID, try again.");
    }
}

/// <summary>
/// Robust loader for parents.json.
/// - Tries several candidate locations.
/// - Supports top-level array of ParentPair, wrapper { "parents": [...] }, or first array property.
/// - If direct deserialization fails, tries to extract dam/sire GUIDs manually from elements with common property names.
/// </summary>
static List<ParentPair> LoadParentPairsFromFile(string file)
{
    string[] candidates = new[]
    {
        file,
        Path.Combine(Directory.GetCurrentDirectory(), file),
        Path.Combine(AppContext.BaseDirectory, file),
        Path.Combine(AppContext.BaseDirectory, "tools", "BreedingSeeder", file),
        Path.Combine(Directory.GetCurrentDirectory(), "tools", "BreedingSeeder", file),
        Path.Combine(Directory.GetCurrentDirectory(), "BreedingSeeder", file)
    };

    string? foundPath = candidates.FirstOrDefault(File.Exists);
    if (foundPath == null)
    {
        Console.WriteLine($"File not found in candidate locations: {string.Join(", ", candidates)}");
        return new List<ParentPair>();
    }

    Console.WriteLine($"Loading parents from: {foundPath}");

    string json = File.ReadAllText(foundPath);
    JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };

    // 1) Try to parse document and find an array element to deserialize
    try
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        // Candidate property names that commonly wrap arrays
        string[] wrapperNames = new[] { "parents", "items", "pairs", "data", "result" };

        // If wrapper object, check known names first
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (string name in wrapperNames)
            {
                if (root.TryGetProperty(name, out JsonElement el) && el.ValueKind == JsonValueKind.Array)
                {
                    var parsed = TryDeserializeArrayElement(el, options);
                    if (parsed.Count > 0) return parsed;
                }
            }

            // Fallback: first array property
            foreach (JsonProperty prop in root.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Array)
                {
                    var parsed = TryDeserializeArrayElement(prop.Value, options);
                    if (parsed.Count > 0) return parsed;
                }
            }
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            var parsed = TryDeserializeArrayElement(root, options);
            if (parsed.Count > 0) return parsed;
        }
    }
    catch (JsonException je)
    {
        Console.WriteLine($"JSON parse failed: {je.Message}");
    }

    // 2) Last-resort: direct deserialize whole document as List<ParentPair>
    try
    {
        List<ParentPair>? list = JsonSerializer.Deserialize<List<ParentPair>>(json, options);
        if (list is { Count: > 0 }) return list;
    }
    catch { /* ignore */ }

    Console.WriteLine("Unable to parse parents.json into parent pairs.");
    return new List<ParentPair>();

    static List<ParentPair> TryDeserializeArrayElement(JsonElement arrayEl, JsonSerializerOptions options)
    {
        var results = new List<ParentPair>();

        if (arrayEl.ValueKind != JsonValueKind.Array) return results;

        string raw = arrayEl.GetRawText();

        // Try direct deserialization to ParentPair[]
        try
        {
            List<ParentPair>? asPairs = JsonSerializer.Deserialize<List<ParentPair>>(raw, options);
            if (asPairs is { Count: > 0 }) return asPairs;
        }
        catch { /* ignore */ }

        // Fallback: enumerate elements and extract dam/sire GUIDs flexibly
        foreach (JsonElement item in arrayEl.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Object)
            {
                Guid? dam = null;
                Guid? sire = null;

                foreach (JsonProperty prop in item.EnumerateObject())
                {
                    string name = prop.Name;
                    string? val = prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() : null;
                    if (val is null) continue;

                    if (string.Equals(name, "damId", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "dam", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "femaleId", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(name, "motherId", StringComparison.OrdinalIgnoreCase))
                    {
                        if (Guid.TryParse(val, out Guid g)) dam = g;
                    }
                    else if (string.Equals(name, "sireId", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(name, "sire", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(name, "maleId", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(name, "fatherId", StringComparison.OrdinalIgnoreCase))
                    {
                        if (Guid.TryParse(val, out Guid g)) sire = g;
                    }
                }

                if (dam.HasValue && sire.HasValue)
                {
                    results.Add(new ParentPair(dam.Value, sire.Value));
                }
            }
            else if (item.ValueKind == JsonValueKind.Array && item.GetArrayLength() >= 2)
            {
                // support arrays like ["damGuid","sireGuid"]
                string? a = item[0].ValueKind == JsonValueKind.String ? item[0].GetString() : null;
                string? b = item[1].ValueKind == JsonValueKind.String ? item[1].GetString() : null;
                if (a != null && b != null && Guid.TryParse(a, out Guid g1) && Guid.TryParse(b, out Guid g2))
                {
                    results.Add(new ParentPair(g1, g2));
                }
            }
        }

        return results;
    }
}

internal record BreedRequestDto(Guid UserId, Guid SireId, Guid DamId);

internal record ParentPair(
    [property: JsonPropertyName("damId")] Guid DamId,
    [property: JsonPropertyName("sireId")] Guid SireId);
