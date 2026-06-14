using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.BtttrPosters.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public bool EnableAutomaticFetching { get; set; } = false;
        public string PosterLanguage { get; set; } = "en";
        public bool FallbackToTmdbText { get; set; } = true;
        public bool TrendTags { get; set; } = true;
        public bool QualityTags { get; set; } = false;
        public bool ShowGenre { get; set; } = true;
        public bool ShowRating { get; set; } = true;
        public bool ShowAgeRating { get; set; } = false;
        public string RatingSource { get; set; } = "avg";
    }
}
