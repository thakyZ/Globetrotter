using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace Globetrotter {
    [Serializable]
    internal class Configuration : IPluginConfiguration {
        private DalamudPluginInterface _pi;

        public int Version { get; set; } = 1;

        public bool ShowOnHover { get; set; } = true;
        public bool ShowOnOpen { get; set; } = true;

        public void Initialize(DalamudPluginInterface pi) {
            this._pi = pi ?? throw new ArgumentNullException(nameof(pi), "DalamudPluginInterface cannot be null");
        }

        public void Save() {
            this._pi.SavePluginConfig(this);
        }
    }
}
