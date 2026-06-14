using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.BtttrPosters.Configuration;

namespace Jellyfin.Plugin.BtttrPosters
{
    public class BtttrImageProvider : IRemoteImageProvider, IHasOrder
    {
        private const string BtttrOrigin = "https://btttr.cc";
        private const string CinemetaOrigin = "https://v3-cinemeta.strem.io";

        private static readonly HashSet<string> ValidRatingSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "avg",
            "IM",
            "TM",
            "RT",
            "MC",
            "TR",
            "LB",
            "RE"
        };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<BtttrImageProvider> _logger;

        public BtttrImageProvider(IHttpClientFactory httpClientFactory, ILogger<BtttrImageProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public string Name => "Btttr Posters";

        public int Order => 0; // Highest priority - displays as the first choice

        public bool Supports(BaseItem item)
        {
            return IsProviderEnabled() && (item is Movie || item is Series);
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return IsProviderEnabled() ? new[] { ImageType.Primary } : Array.Empty<ImageType>();
        }

        public Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            return GetImagesInternal(item, cancellationToken);
        }

        private async Task<IEnumerable<RemoteImageInfo>> GetImagesInternal(BaseItem item, CancellationToken cancellationToken)
        {
            var images = new List<RemoteImageInfo>();
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            if (!config.EnableAutomaticFetching)
            {
                _logger.LogDebug("Btttr Image Provider is disabled by configuration.");
                return images;
            }

            string itemType = item is Movie ? "movie" : "series";

            _logger.LogInformation("Processing Btttr Image Provider for item: {Name}", item.Name);
            string? targetId = await ResolveImdbId(item, itemType, config, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(targetId))
            {
                _logger.LogWarning("Btttr Image Provider: IMDb ID not found for item: {Name}. Cannot fetch btttr.cc custom poster.", item.Name);
                return images;
            }

            string btttrUrl = BuildPosterUrl(itemType, targetId, config);

            _logger.LogInformation("Generating Btttr.cc URL format: {Url}", btttrUrl);

            images.Add(new RemoteImageInfo
            {
                ProviderName = Name,
                Url = btttrUrl,
                ThumbnailUrl = btttrUrl,
                Type = ImageType.Primary
            });

            return images;
        }

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching custom poster from Btttr: {Url}", url);
            var client = _httpClientFactory.CreateClient(Name);
            return client.GetAsync(url, cancellationToken);
        }

        private async Task<string?> ResolveImdbId(BaseItem item, string itemType, PluginConfiguration config, CancellationToken cancellationToken)
        {
            string? imdbId = NormalizeImdbId(item.GetProviderId(MetadataProvider.Imdb));
            if (!string.IsNullOrEmpty(imdbId))
            {
                return imdbId;
            }

            if (!config.FallbackToTmdbText)
            {
                return null;
            }

            string? tmdbId = item.GetProviderId(MetadataProvider.Tmdb);
            if (string.IsNullOrWhiteSpace(tmdbId))
            {
                return null;
            }

            string query = item.Name ?? string.Empty;
            if (item.ProductionYear.HasValue)
            {
                query += " " + item.ProductionYear.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                return null;
            }

            try
            {
                var client = _httpClientFactory.CreateClient($"{Name} Cinemeta");
                client.Timeout = TimeSpan.FromSeconds(8);

                string searchUrl = $"{CinemetaOrigin}/catalog/{itemType}/top/search={Uri.EscapeDataString(query)}.json";
                string searchJson = await client.GetStringAsync(searchUrl, cancellationToken).ConfigureAwait(false);

                using JsonDocument searchDocument = JsonDocument.Parse(searchJson);
                if (!searchDocument.RootElement.TryGetProperty("metas", out JsonElement metas) || metas.ValueKind != JsonValueKind.Array)
                {
                    return null;
                }

                int checkedCandidates = 0;
                foreach (JsonElement meta in metas.EnumerateArray())
                {
                    if (checkedCandidates >= 8)
                    {
                        break;
                    }

                    string? candidateId = GetJsonString(meta, "imdb_id") ?? GetJsonString(meta, "id");
                    candidateId = NormalizeImdbId(candidateId);
                    if (string.IsNullOrEmpty(candidateId))
                    {
                        continue;
                    }

                    checkedCandidates++;
                    if (await CandidateMatchesTmdb(client, itemType, candidateId, tmdbId, cancellationToken).ConfigureAwait(false))
                    {
                        _logger.LogInformation("Resolved TMDB ID {TmdbId} to IMDb ID {ImdbId} for {Name}", tmdbId, candidateId, item.Name);
                        return candidateId;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to resolve TMDB ID {TmdbId} to an IMDb ID for {Name}", tmdbId, item.Name);
            }

            _logger.LogWarning("Btttr Image Provider: TMDB fallback could not resolve TMDB ID {TmdbId} for item: {Name}", tmdbId, item.Name);
            return null;
        }

        private static async Task<bool> CandidateMatchesTmdb(HttpClient client, string itemType, string imdbId, string tmdbId, CancellationToken cancellationToken)
        {
            string metaUrl = $"{CinemetaOrigin}/meta/{itemType}/{Uri.EscapeDataString(imdbId)}.json";
            string metaJson = await client.GetStringAsync(metaUrl, cancellationToken).ConfigureAwait(false);

            using JsonDocument metaDocument = JsonDocument.Parse(metaJson);
            if (!metaDocument.RootElement.TryGetProperty("meta", out JsonElement meta))
            {
                return false;
            }

            string? moviedbId = GetJsonString(meta, "moviedb_id");
            return string.Equals(moviedbId, tmdbId, StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildPosterUrl(string itemType, string imdbId, PluginConfiguration config)
        {
            string tag = config.TrendTags ? "auto" : "none";
            string bottomCode = GetBottomCode(config);
            string btttrUrl = $"{BtttrOrigin}/poster/{itemType}/{imdbId}/{tag}~{bottomCode}.png";

            var queryParams = new List<string>();
            string language = config.PosterLanguage ?? "en";
            if (!string.Equals(language, "none", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(language, "en", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(language))
            {
                queryParams.Add($"lang={Uri.EscapeDataString(language)}");
            }

            if (config.QualityTags)
            {
                queryParams.Add("q=1");
            }

            string ratingSource = config.RatingSource ?? "avg";
            if (config.ShowRating
                && ValidRatingSources.Contains(ratingSource)
                && !string.Equals(ratingSource, "avg", StringComparison.OrdinalIgnoreCase))
            {
                queryParams.Add($"rs={Uri.EscapeDataString(ratingSource)}");
            }

            if (queryParams.Count > 0)
            {
                btttrUrl += "?" + string.Join("&", queryParams);
            }

            return btttrUrl;
        }

        private static string GetBottomCode(PluginConfiguration config)
        {
            string bottomCode = string.Empty;
            if (config.ShowGenre)
            {
                bottomCode += "g";
            }

            if (config.ShowRating)
            {
                bottomCode += "r";
            }

            if (config.ShowAgeRating)
            {
                bottomCode += "a";
            }

            return bottomCode.Length == 0 ? "n" : bottomCode;
        }

        private static string? NormalizeImdbId(string? imdbId)
        {
            if (string.IsNullOrWhiteSpace(imdbId))
            {
                return null;
            }

            imdbId = imdbId.Trim();
            return imdbId.StartsWith("tt", StringComparison.OrdinalIgnoreCase) ? imdbId : "tt" + imdbId;
        }

        private static string? GetJsonString(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement value))
            {
                return null;
            }

            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.GetRawText(),
                _ => null
            };
        }

        private static bool IsProviderEnabled()
        {
            return Plugin.Instance?.Configuration?.EnableAutomaticFetching == true;
        }
    }
}
