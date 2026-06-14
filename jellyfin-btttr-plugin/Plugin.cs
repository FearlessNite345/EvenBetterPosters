using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Jellyfin.Plugin.BtttrPosters.Configuration;

namespace Jellyfin.Plugin.BtttrPosters
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public override string Name => "Even Better Posters";

        public override Guid Id => Guid.Parse("9d283167-31b1-492b-b2fc-2564c5cb9cc8");

        public static Plugin? Instance { get; private set; }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = this.Name + " Configuration",
                    EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html"
                }
            };
        }
    }
}
