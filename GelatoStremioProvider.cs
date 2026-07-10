using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Gelato;

public class GelatoStremioProvider(
    string baseUrl,
    IHttpClientFactory http,
    ILogger<GelatoStremioProvider> log
)
{
    private sealed class AddonEndpoint
    {
        public required int Index { get; init; }
        public required string BaseUrl { get; init; }
        public StremioManifest? Manifest { get; set; }

        public string DisplayName =>
            string.IsNullOrWhiteSpace(Manifest?.Name) ? $"Addon {Index + 1}" : Manifest.Name;
    }

    private sealed record CatalogRoute(AddonEndpoint Endpoint, string OriginalCatalogId);

    private readonly List<AddonEndpoint> _addons = ParseBaseUrls(baseUrl);
    private readonly Dictionary<string, CatalogRoute> _catalogRoutes =
        new(StringComparer.OrdinalIgnoreCase);

    private StremioManifest? _manifest;
    private StremioCatalog? _movieSearchCatalog;
    private StremioCatalog? _seriesSearchCatalog;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly TimeSpan MetaCacheTtl = TimeSpan.FromMinutes(5);
    private readonly System.Collections.Concurrent.ConcurrentDictionary<
        string,
        (StremioMeta Meta, DateTime Expiry)
    > _metaCache = new(StringComparer.OrdinalIgnoreCase);

    private static List<AddonEndpoint> ParseBaseUrls(string raw)
    {
        return raw
            .Split(new[] { '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizeBaseUrl)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select((url, index) => new AddonEndpoint { Index = index, BaseUrl = url })
            .ToList();
    }

    private static string NormalizeBaseUrl(string value)
    {
        var url = value.Trim().TrimEnd('/');
        if (url.EndsWith("/manifest.json", StringComparison.OrdinalIgnoreCase))
            url = url[..^"/manifest.json".Length];
        return url.TrimEnd('/');
    }

    private StremioMeta? GetCachedMeta(string key)
    {
        if (_metaCache.TryGetValue(key, out var entry) && entry.Expiry > DateTime.UtcNow)
            return entry.Meta;
        _metaCache.TryRemove(key, out _);
        return null;
    }

    private HttpClient NewClient()
    {
        var client = http.CreateClient(nameof(GelatoStremioProvider));
        client.Timeout = TimeSpan.FromSeconds(30);
        return client;
    }

    private static string BuildUrl(
        AddonEndpoint addon,
        string[] segments,
        IEnumerable<string>? extras = null
    )
    {
        var parts = segments.Select(Uri.EscapeDataString).ToArray();
        var path = string.Join("/", parts);

        var extrasPart = string.Empty;
        if (extras is not null)
        {
            var values = extras.ToList();
            extrasPart = values.Count == 0 ? string.Empty : "/" + string.Join("&", values);
        }

        var url = $"{addon.BaseUrl}/{path}{extrasPart}.json";
        return url.Replace("%3A", ":").Replace("%3a", ":");
    }

    private async Task<T?> GetJsonAsync<T>(AddonEndpoint addon, string url)
    {
        log.LogDebug("[{Addon}] requesting {Url}", addon.DisplayName, url);

        using var client = NewClient();
        using var response = await client.GetAsync(url).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"HTTP {response.StatusCode}: {response.ReasonPhrase}",
                null,
                response.StatusCode
            );
        }

        await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOpts).ConfigureAwait(false);
    }

    private async Task<StremioManifest?> LoadManifestAsync(AddonEndpoint addon)
    {
        try
        {
            var manifest = await GetJsonAsync<StremioManifest>(
                    addon,
                    $"{addon.BaseUrl}/manifest.json"
                )
                .ConfigureAwait(false);
            addon.Manifest = manifest;
            return manifest;
        }
        catch (Exception ex)
        {
            addon.Manifest = null;
            log.LogWarning(ex, "Cannot load Stremio manifest from {BaseUrl}", addon.BaseUrl);
            return null;
        }
    }

    public async Task<StremioManifest?> GetManifestAsync(bool force = false)
    {
        if (!force && _manifest is not null)
            return _manifest;

        if (_addons.Count == 0)
        {
            log.LogWarning("No Stremio addon URLs are configured");
            return null;
        }

        await Task.WhenAll(_addons.Select(LoadManifestAsync)).ConfigureAwait(false);
        var available = _addons.Where(addon => addon.Manifest is not null).ToList();
        if (available.Count == 0)
            return null;

        _catalogRoutes.Clear();
        var combinedCatalogs = new List<StremioCatalog>();

        foreach (var addon in available)
        {
            foreach (var catalog in addon.Manifest!.Catalogs)
            {
                var routedId = $"a{addon.Index}--{catalog.Id}";
                _catalogRoutes[routedId] = new CatalogRoute(addon, catalog.Id);
                combinedCatalogs.Add(
                    new StremioCatalog
                    {
                        Id = routedId,
                        Type = catalog.Type,
                        Name = $"{addon.DisplayName} — {catalog.Name}",
                        Extra = catalog.Extra,
                    }
                );
            }
        }

        _movieSearchCatalog = combinedCatalogs
            .Where(c =>
                string.Equals(c.Type, "movie", StringComparison.OrdinalIgnoreCase)
                && c.IsSearchCapable()
            )
            .OrderBy(c => c.Id.Contains("people", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

        _seriesSearchCatalog = combinedCatalogs
            .Where(c =>
                string.Equals(c.Type, "series", StringComparison.OrdinalIgnoreCase)
                && c.IsSearchCapable()
            )
            .OrderBy(c => c.Id.Contains("people", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

        _manifest = new StremioManifest
        {
            Id = "org.gelato.universal",
            Name = "Gelato Universal",
            Version = "1.0.0",
            Description = "Combined manifest generated from configured Stremio addons",
            Catalogs = combinedCatalogs,
            Types = available
                .SelectMany(a => a.Manifest!.Types)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Resources = CombineResources(available),
        };

        log.LogInformation(
            "Loaded {Loaded}/{Configured} Stremio addons",
            available.Count,
            _addons.Count
        );
        return _manifest;
    }

    private static List<StremioResource> CombineResources(IEnumerable<AddonEndpoint> addons)
    {
        var resources = new Dictionary<string, StremioResource>(StringComparer.OrdinalIgnoreCase);

        foreach (var addon in addons)
        {
            var manifest = addon.Manifest!;
            foreach (var resource in manifest.Resources)
            {
                if (!resources.TryGetValue(resource.Name, out var combined))
                {
                    combined = new StremioResource { Name = resource.Name };
                    resources[resource.Name] = combined;
                }

                var types = resource.Types.Count > 0 ? resource.Types : manifest.Types;
                combined.Types = combined.Types
                    .Concat(types)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var prefixes =
                    resource.IdPrefixes.Count > 0 ? resource.IdPrefixes : manifest.IdPrefixes;
                combined.IdPrefixes = combined.IdPrefixes
                    .Concat(prefixes)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
        }

        return resources.Values.ToList();
    }

    public async Task<bool> IsReady()
    {
        var manifest = await GetManifestAsync().ConfigureAwait(false);
        return manifest is not null;
    }

    public async Task<StremioMeta?> GetMetaAsync(
        string id,
        StremioMediaType mediaType,
        TimeSpan? ttl = null
    )
    {
        var cacheKey = $"{mediaType}:{id}";
        var cached = GetCachedMeta(cacheKey);
        if (cached is not null)
            return cached;

        await GetManifestAsync().ConfigureAwait(false);
        foreach (var addon in _addons)
        {
            if (addon.Manifest?.SupportsResource("meta", mediaType, id) != true)
                continue;

            try
            {
                var url = BuildUrl(addon, ["meta", mediaType.ToString().ToLowerInvariant(), id]);
                var response = await GetJsonAsync<StremioMetaResponse>(addon, url)
                    .ConfigureAwait(false);
                if (response?.Meta is not { } meta)
                    continue;

                _metaCache[cacheKey] = (meta, DateTime.UtcNow.Add(ttl ?? MetaCacheTtl));
                return meta;
            }
            catch (Exception ex)
            {
                log.LogWarning(
                    ex,
                    "Metadata request failed for {Addon} and {Id}",
                    addon.DisplayName,
                    id
                );
            }
        }

        return null;
    }

    public async Task<StremioMeta?> GetMetaAsync(BaseItem item)
    {
        var id = item.GetProviderId("Imdb");
        if (id is null)
        {
            log.LogWarning("GetMetaAsync: {Name} has no imdb ID", item.Name);
            id = item.GetProviderId("Tmdb");
            if (id is null)
            {
                log.LogWarning("GetMetaAsync: {Name} has no imdb and tmdb ID", item.Name);
                return null;
            }
            id = $"tmdb:{id}";
        }

        return await GetMetaAsync(id, item.GetBaseItemKind().ToStremio()).ConfigureAwait(false);
    }

    private static string GetTmdbApiKey()
    {
        const string defaultKey = "4219e299c89411838049ab0dab19ebd5";
        try
        {
            var pluginType = Type.GetType(
                "MediaBrowser.Providers.Plugins.Tmdb.Plugin, Jellyfin.Providers",
                throwOnError: false
            );
            var instance = pluginType?.GetProperty("Instance")?.GetValue(null);
            var cfg = instance?.GetType().GetProperty("Configuration")?.GetValue(instance);
            var key = cfg?.GetType().GetProperty("TmdbApiKey")?.GetValue(cfg) as string;
            return string.IsNullOrWhiteSpace(key) ? defaultKey : key;
        }
        catch
        {
            return defaultKey;
        }
    }

    public async Task EnrichDigitalReleaseDateAsync(
        StremioMeta meta,
        CancellationToken cancellationToken
    )
    {
        if (meta.App_Extras?.ReleaseDates is not null)
            return;

        var tmdbId = meta.GetProviderIds().GetValueOrDefault(nameof(MetadataProvider.Tmdb));
        if (string.IsNullOrWhiteSpace(tmdbId))
            return;

        var apiKey = GetTmdbApiKey();
        var url =
            $"https://api.themoviedb.org/3/movie/{Uri.EscapeDataString(tmdbId)}/release_dates?api_key={apiKey}";

        try
        {
            using var client = http.CreateClient(nameof(GelatoStremioProvider));
            client.Timeout = TimeSpan.FromSeconds(10);
            var response = await client.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
            var container = JsonSerializer.Deserialize<TmdbReleaseDatesContainer>(response, JsonOpts);
            if (container is not null)
            {
                meta.App_Extras ??= new StremioAppExtras();
                meta.App_Extras.ReleaseDates = container;
            }
        }
        catch (Exception ex)
        {
            log.LogDebug(ex, "EnrichDigitalReleaseDate failed for tmdb:{TmdbId}", tmdbId);
        }
    }

    public async Task<List<StremioStream>> GetStreamsAsync(StremioUri uri)
    {
        await GetManifestAsync().ConfigureAwait(false);

        var providers = _addons
            .Where(addon =>
                addon.Manifest?.SupportsResource("stream", uri.MediaType, uri.ExternalId) == true
            )
            .ToList();

        var results = await Task.WhenAll(
                providers.Select(addon => GetStreamsFromAddonAsync(addon, uri))
            )
            .ConfigureAwait(false);

        return results
            .SelectMany(streams => streams)
            .GroupBy(GetStreamIdentity, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private async Task<List<StremioStream>> GetStreamsFromAddonAsync(
        AddonEndpoint addon,
        StremioUri uri
    )
    {
        try
        {
            var url = BuildUrl(
                addon,
                ["stream", uri.MediaType.ToString().ToLowerInvariant(), uri.ExternalId]
            );
            var response = await GetJsonAsync<StremioStreamsResponse>(addon, url)
                .ConfigureAwait(false);
            var streams = response?.Streams ?? [];

            foreach (var stream in streams)
            {
                if (string.IsNullOrWhiteSpace(stream.Name))
                    stream.Name = addon.DisplayName;
                else if (!stream.Name.Contains(addon.DisplayName, StringComparison.OrdinalIgnoreCase))
                    stream.Name = $"{addon.DisplayName} • {stream.Name}";
            }

            return streams;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Stream request failed for {Addon}", addon.DisplayName);
            return [];
        }
    }

    private static string GetStreamIdentity(StremioStream stream)
    {
        if (!string.IsNullOrWhiteSpace(stream.Url))
            return $"url:{stream.Url.Trim()}";
        if (!string.IsNullOrWhiteSpace(stream.InfoHash))
            return $"torrent:{stream.InfoHash}:{stream.FileIdx}";
        return $"fallback:{stream.Name}:{stream.Title}:{stream.BehaviorHints?.Filename}";
    }

    public async Task<List<StremioSubtitle>> GetSubtitlesAsync(
        string id,
        StremioMediaType mediaType
    )
    {
        await GetManifestAsync().ConfigureAwait(false);
        var providers = _addons
            .Where(addon => addon.Manifest?.SupportsResource("subtitles", mediaType, id) == true)
            .ToList();

        var results = await Task.WhenAll(
                providers.Select(async addon =>
                {
                    try
                    {
                        var url = BuildUrl(
                            addon,
                            ["subtitles", mediaType.ToString().ToLowerInvariant(), id]
                        );
                        var response = await GetJsonAsync<StremioSubtitleResponse>(addon, url)
                            .ConfigureAwait(false);
                        return response?.Subtitles ?? [];
                    }
                    catch (Exception ex)
                    {
                        log.LogWarning(ex, "Subtitle request failed for {Addon}", addon.DisplayName);
                        return [];
                    }
                })
            )
            .ConfigureAwait(false);

        return results
            .SelectMany(subtitles => subtitles)
            .Where(subtitle => !string.IsNullOrWhiteSpace(subtitle.Url))
            .GroupBy(
                subtitle => $"{subtitle.Url}|{subtitle.Lang}|{subtitle.LangCode}",
                StringComparer.OrdinalIgnoreCase
            )
            .Select(group => group.First())
            .ToList();
    }

    public async Task<IReadOnlyList<StremioMeta>> GetCatalogMetasAsync(
        string id,
        string mediaType,
        string? search = null,
        int? skip = null
    )
    {
        await GetManifestAsync().ConfigureAwait(false);

        CatalogRoute? route = null;
        if (_catalogRoutes.TryGetValue(id, out var routed))
        {
            route = routed;
        }
        else
        {
            var fallback = _addons.FirstOrDefault(addon =>
                addon.Manifest?.Catalogs.Any(c =>
                    string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(c.Type, mediaType, StringComparison.OrdinalIgnoreCase)
                ) == true
            );
            if (fallback is not null)
                route = new CatalogRoute(fallback, id);
        }

        if (route is null)
        {
            log.LogWarning("No addon owns catalog {CatalogId} ({MediaType})", id, mediaType);
            return [];
        }

        var extras = new List<string>();
        if (!string.IsNullOrWhiteSpace(search))
            extras.Add($"search={Uri.EscapeDataString(search)}");
        if (skip is > 0)
            extras.Add($"skip={skip}");

        try
        {
            var url = BuildUrl(
                route.Endpoint,
                ["catalog", mediaType.ToLowerInvariant(), route.OriginalCatalogId],
                extras
            );
            var response = await GetJsonAsync<StremioCatalogResponse>(route.Endpoint, url)
                .ConfigureAwait(false);
            return response?.Metas ?? [];
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Catalog request failed for {Addon}", route.Endpoint.DisplayName);
            return [];
        }
    }

    public async Task<IReadOnlyList<StremioMeta>> SearchAsync(
        string query,
        StremioMediaType mediaType,
        int? skip = null
    )
    {
        var manifest = await GetManifestAsync().ConfigureAwait(false);
        if (manifest is null)
            return [];

        var catalog = mediaType switch
        {
            StremioMediaType.Movie => _movieSearchCatalog,
            StremioMediaType.Series => _seriesSearchCatalog,
            _ => null,
        };

        if (catalog is null)
        {
            log.LogError(
                "No search-capable {MediaType} catalog is configured. Put a metadata addon such as Cinemeta first.",
                mediaType
            );
            return [];
        }

        return await GetCatalogMetasAsync(catalog.Id, mediaType.ToString(), query, skip)
            .ConfigureAwait(false);
    }
}


#region Request Models

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
public class StremioManifest
{
    public string Name { get; set; } = "";
    public string Id { get; set; } = "";
    public string Version { get; set; } = "";
    public string? Description { get; set; }
    public List<StremioCatalog> Catalogs { get; set; } = new();
    public List<StremioResource> Resources { get; set; } = new();
    public List<string> Types { get; set; } = new();
    public List<string> IdPrefixes { get; set; } = new();
    public string? Background { get; set; }
    public string? Logo { get; set; }
    public StremioBehaviorHints? BehaviorHints { get; set; }
    public List<StremioCatalog> AddonCatalogs { get; set; } = new();

    public bool SupportsResource(string name, StremioMediaType mediaType, string? id = null)
    {
        var resource = Resources.FirstOrDefault(r =>
            string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)
        );
        if (resource is null)
            return false;

        var type = mediaType.ToString().ToLowerInvariant();
        var supportedTypes = resource.Types.Count > 0 ? resource.Types : Types;
        if (
            supportedTypes.Count > 0
            && !supportedTypes.Contains(type, StringComparer.OrdinalIgnoreCase)
        )
            return false;

        if (string.Equals(name, "catalog", StringComparison.OrdinalIgnoreCase) || id is null)
            return true;

        var prefixes = resource.IdPrefixes.Count > 0 ? resource.IdPrefixes : IdPrefixes;
        return prefixes.Count == 0
            || prefixes.Any(prefix => id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}

public class StremioCatalog
{
    // we dont cast to enum cause types is not a static set
    public string Type { get; set; } = "";
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public List<StremioExtra> Extra { get; set; } = new();

    public bool IsSearchCapable()
    {
        return Extra.Any(e => string.Equals(e.Name, "search", StringComparison.OrdinalIgnoreCase));
    }

    // should not have required extras
    public bool IsImportable()
    {
        return !Extra.Any(e => e.IsRequired == true);
    }
}

public class StremioExtra
{
    public string Name { get; set; } = "";
    public bool IsRequired { get; set; }
    public List<string> Options { get; set; } = new();
}

[JsonConverter(typeof(StremioResourceConverter))]
public class StremioResource
{
    public string Name { get; set; } = "";
    public List<string> Types { get; set; } = new();
    public List<string> IdPrefixes { get; set; } = new();
}

public sealed class StremioResourceConverter : JsonConverter<StremioResource>
{
    public override StremioResource Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.String)
            return new StremioResource { Name = reader.GetString() ?? "" };

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var resource = new StremioResource();

        if (root.TryGetProperty("name", out var name))
            resource.Name = name.GetString() ?? "";
        if (root.TryGetProperty("types", out var types) && types.ValueKind == JsonValueKind.Array)
            resource.Types = types
                .EnumerateArray()
                .Select(value => value.GetString())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Cast<string>()
                .ToList();
        if (
            root.TryGetProperty("idPrefixes", out var prefixes)
            && prefixes.ValueKind == JsonValueKind.Array
        )
            resource.IdPrefixes = prefixes
                .EnumerateArray()
                .Select(value => value.GetString())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Cast<string>()
                .ToList();

        return resource;
    }

    public override void Write(
        Utf8JsonWriter writer,
        StremioResource value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartObject();
        writer.WriteString("name", value.Name);
        if (value.Types.Count > 0)
        {
            writer.WritePropertyName("types");
            JsonSerializer.Serialize(writer, value.Types, options);
        }
        if (value.IdPrefixes.Count > 0)
        {
            writer.WritePropertyName("idPrefixes");
            JsonSerializer.Serialize(writer, value.IdPrefixes, options);
        }
        writer.WriteEndObject();
    }
}

public class StremioCatalogResponse
{
    public List<StremioMeta>? Metas { get; set; }
}

public struct StremioSubtitle
{
    public string Id { get; set; }
    public string Url { get; set; }
    public string? Lang { get; set; }
    public int? SubId { get; set; }
    public bool? AiTranslated { get; set; }
    public bool? FromTrusted { get; set; }
    public int? UploaderId { get; set; }

    [JsonPropertyName("lang_code")]
    public string? LangCode { get; set; }
    public string? Title { get; set; }
    public string? Moviehash { get; set; }

    public string? TwoLetterISOLanguageName()
    {
        var lng = Lang ?? LangCode;
        if (!string.IsNullOrWhiteSpace(lng))
        {
            // If the input is 3 characters, try to convert it to a 2-letter ISO code
            if (lng.Length == 3)
            {
                try
                {
                    CultureInfo culture = CultureInfo.GetCultureInfoByIetfLanguageTag(
                        lng.ToLower()
                    );
                    lng = culture.TwoLetterISOLanguageName;
                }
                catch (CultureNotFoundException)
                {
                    // If the 3-letter code is invalid, return null or handle as needed
                    return null;
                }
            }
            return lng.ToLower();
        }

        return null;
    }
}

public struct StremioSubtitleResponse
{
    public List<StremioSubtitle> Subtitles { get; set; }
}

public class StremioMetaResponse
{
    public StremioMeta Meta { get; set; } = null!;
}

public class StremioMeta
{
    public required string Id { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StremioMediaType Type { get; set; } = StremioMediaType.Unknown;
    public string? Name { get; set; }
    public string? Title { get; set; }
    public string? Poster { get; set; }
    public List<string>? Genres { get; set; }

    // sometimes string, sometimes number... disable for now
    // public string? ImdbRating { get; set; }
    [JsonConverter(typeof(NullableStringLenientConverter))]
    public string? ReleaseInfo { get; set; }
    public string? Description { get; set; }
    public string? Overview { get; set; }
    public List<StremioTrailer>? Trailers { get; set; }
    public List<StremioLink>? Links { get; set; }
    public string? Background { get; set; }
    public string? Logo { get; set; }
    public List<StremioMeta>? Videos { get; set; }
    public string? Runtime { get; set; }
    public string? Country { get; set; }

    [JsonConverter(typeof(StringOrArrayConverter))]
    public string? Director { get; set; }

    [JsonConverter(typeof(StringOrArrayConverter))]
    public string? Writer { get; set; }
    public string? LandscapePoster { get; set; }

    [JsonConverter(typeof(NullableFloatLenientConverter))]
    public float? ImdbRating { get; set; }

    public StremioBehaviorHints? BehaviorHints { get; set; }
    public List<string>? Genre { get; set; }

    [JsonPropertyName("imdb_id")]
    public string? ImdbId { get; set; }
    public DateTime? Released { get; set; }

    [JsonConverter(typeof(SafeStringEnumConverter<StremioStatus>))]
    // ReSharper disable once MemberCanBePrivate.Global
    public StremioStatus? Status { get; set; } = StremioStatus.Unknown;

    [JsonConverter(typeof(NullableIntLenientConverter))]
    public int? Year { get; set; }
    public string? Slug { get; set; }
    public List<StremioTrailerStream>? TrailerStreams { get; set; }

    // ReSharper disable once InconsistentNaming
    public StremioAppExtras? App_Extras { get; set; }
    public string? Thumbnail { get; set; }
    public int? Episode { get; set; }
    public int? Season { get; set; }
    public int? Number { get; set; }
    public DateTime? FirstAired { get; set; }
    public Guid? Guid { get; set; }

    public string? TvdbEpisodeId()
    {
        if (!Uri.TryCreate(Thumbnail, UriKind.Absolute, out var uri))
            return null;

        if (!uri.Host.Contains("thetvdb.com", StringComparison.OrdinalIgnoreCase))
            return null;

        var lastSegment = uri.Segments.Last().TrimEnd('/');

        return int.TryParse(lastSegment, out _) ? lastSegment : null;
    }

    public string GetName()
    {
        if (!string.IsNullOrWhiteSpace(Title))
        {
            return Title;
        }
        if (!string.IsNullOrWhiteSpace(Name))
        {
            return Name;
        }
        return "";
    }

    public Dictionary<string, string> GetProviderIds()
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(Id))
        {
            if (Id.StartsWith("tmdb:", StringComparison.OrdinalIgnoreCase))
            {
                dict[nameof(MetadataProvider.Tmdb)] = Id["tmdb:".Length..];
            }
            else if (Id.StartsWith("tt", StringComparison.OrdinalIgnoreCase))
            {
                dict[nameof(MetadataProvider.Imdb)] = Id;
            }
        }

        if (!string.IsNullOrWhiteSpace(ImdbId))
        {
            dict[nameof(MetadataProvider.Imdb)] = ImdbId;
        }

        return dict;
    }

    public int? GetYear()
    {
        if (Year is not null)
            return Year;

        if (Released is { } dt)
            return dt.Year;

        // "2007-2019", "2020-", or "2015"
        if (!string.IsNullOrWhiteSpace(ReleaseInfo))
        {
            var s = ReleaseInfo.Trim();

            if (
                s.Length >= 4
                && int.TryParse(s.AsSpan(0, 4), out var startYear)
                && startYear is > 1800 and < 2200
            )
                return startYear;

            var dashIndex = s.IndexOf('-');
            if (
                dashIndex > 0
                && int.TryParse(s[..dashIndex], out var year2)
                && year2 is > 1800 and < 2200
            )
                return year2;

            if (int.TryParse(s, out var plainYear) && plainYear is > 1800 and < 2200)
                return plainYear;
        }

        return null;
    }

    public DateTime? GetPremiereDate()
    {
        if (Released is { } dt)
            return dt;

        var year = GetYear();
        if (year is null)
        {
            return null;
        }
        return new DateTime(year.Value, 1, 1);
    }

    /// <summary>
    /// Returns the earliest digital (TMDB type 4) release date across all countries, or null if unavailable.
    /// </summary>
    public DateTime? GetDigitalReleaseDate()
    {
        var results = App_Extras?.ReleaseDates?.Results;
        if (results is null)
            return null;

        DateTime? earliest = null;
        foreach (var country in results)
        {
            if (country.ReleaseDates is null)
                continue;
            foreach (var rd in country.ReleaseDates)
            {
                if (rd.Type == 4 && rd.ReleaseDate.HasValue)
                {
                    if (earliest is null || rd.ReleaseDate.Value < earliest.Value)
                        earliest = rd.ReleaseDate.Value;
                }
            }
        }
        return earliest;
    }

    public bool IsValid()
    {
        if (!string.IsNullOrWhiteSpace(Url))
        {
            if (!Uri.TryCreate(Url, UriKind.Absolute, out var uri))
                return false;
            return !(uri.PathAndQuery == "/" || string.IsNullOrEmpty(uri.PathAndQuery));
        }

        return !string.IsNullOrWhiteSpace(InfoHash);
    }

    public bool IsFile()
    {
        return !string.IsNullOrWhiteSpace(Url);
    }

    public bool IsTorrent()
    {
        return !string.IsNullOrWhiteSpace(InfoHash);
    }
}

public class StremioBehaviorHints
{
    public string? BingeGroup { get; set; }
    public string? VideoHash { get; set; }
    public long? VideoSize { get; set; }
    public string? Filename { get; set; }
    public bool Configurable { get; set; }
    public bool ConfigurationRequired { get; set; }
}

public class StremioOptions
{
    public string BaseUrl { get; set; } = "https://your-stremio-addon";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(8);
}

public enum StremioMediaType
{
    Unknown = 0,
    Movie,
    Series,
    Episode, // doesnt exist in stremio. But we wanna know
    Channel,
    Collections,
    Anime,
    Other,
    Tv,
    Events,
}

public enum StremioStatus
{
    Unknown = 0,
    Upcoming,
    Ended,
    Continuing,
}

// ReSharper restore UnusedAutoPropertyAccessor.Global
// ReSharper restore CollectionNeverUpdated.Global
// ReSharper restore ClassNeverInstantiated.Global

#endregion

public class SafeStringEnumConverter<T> : JsonConverter<T>
    where T : struct, Enum
{
    public override T Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            if (Enum.TryParse<T>(s, true, out var value))
                return value;
            if (Enum.TryParse<T>("Unknown", true, out var fallback))
                return fallback;
        }
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt32(out var i) && Enum.IsDefined(typeof(T), i))
                return (T)Enum.ToObject(typeof(T), i);
        }
        reader.Skip();
        return Enum.TryParse<T>("Unknown", true, out var fb) ? fb : default;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}

public sealed class NullableIntLenientConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        switch (r.TokenType)
        {
            case JsonTokenType.Number:
                return r.TryGetInt32(out var i) ? i : null;
            case JsonTokenType.String:
                var s = r.GetString();
                if (string.IsNullOrWhiteSpace(s))
                    return null;

                return int.TryParse(
                    s,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var v
                )
                    ? v
                    : null;
            case JsonTokenType.Null:
            default:
                return null;
        }
    }

    public override void Write(Utf8JsonWriter w, int? v, JsonSerializerOptions o)
    {
        if (v.HasValue)
            w.WriteNumberValue(v.Value);
        else
            w.WriteNullValue();
    }
}

public sealed class NullableStringLenientConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        return r.TokenType switch
        {
            JsonTokenType.String => r.GetString(),
            JsonTokenType.Number => r.TryGetInt64(out var i)
                ? i.ToString(CultureInfo.InvariantCulture)
                : r.GetDouble().ToString(CultureInfo.InvariantCulture),
            _ => null,
        };
    }

    public override void Write(Utf8JsonWriter w, string? v, JsonSerializerOptions o)
    {
        if (v is not null)
            w.WriteStringValue(v);
        else
            w.WriteNullValue();
    }
}

public sealed class NullableFloatLenientConverter : JsonConverter<float?>
{
    public override float? Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        switch (r.TokenType)
        {
            case JsonTokenType.Number:
                return r.TryGetSingle(out var f) ? f : null;
            case JsonTokenType.String:
                var s = r.GetString();
                return float.TryParse(
                    s,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var v
                )
                    ? v
                    : null;
            case JsonTokenType.Null:
            default:
                return null;
        }
    }

    public override void Write(Utf8JsonWriter w, float? v, JsonSerializerOptions o)
    {
        if (v.HasValue)
            w.WriteNumberValue(v.Value);
        else
            w.WriteNullValue();
    }
}

/// <summary>
/// Handles fields that can be either a JSON string or an array of strings.
/// Arrays are joined with ", ". Numbers are coerced to string. Null/other → null.
/// </summary>
public sealed class StringOrArrayConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        switch (r.TokenType)
        {
            case JsonTokenType.String:
                return r.GetString();
            case JsonTokenType.Number:
                return r.TryGetInt64(out var i)
                    ? i.ToString(CultureInfo.InvariantCulture)
                    : r.GetDouble().ToString(CultureInfo.InvariantCulture);
            case JsonTokenType.StartArray:
                var parts = new List<string>();
                while (r.Read() && r.TokenType != JsonTokenType.EndArray)
                {
                    if (r.TokenType == JsonTokenType.String)
                    {
                        var s = r.GetString();
                        if (!string.IsNullOrWhiteSpace(s))
                            parts.Add(s);
                    }
                }
                return parts.Count > 0 ? string.Join(", ", parts) : null;
            default:
                return null;
        }
    }

    public override void Write(Utf8JsonWriter w, string? v, JsonSerializerOptions o)
    {
        if (v is not null)
            w.WriteStringValue(v);
        else
            w.WriteNullValue();
    }
}
