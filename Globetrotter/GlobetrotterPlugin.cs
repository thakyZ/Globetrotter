using Dalamud.Game.Command;
using Dalamud.Plugin;
using System;

namespace Globetrotter {
    public class GlobetrotterPlugin : IDalamudPlugin {
        private bool disposedValue;

        public string Name => "Globetrotter";

        private DalamudPluginInterface pi;
        private Configuration config;
        private PluginUI ui;
        private TreasureMaps maps;

        public void Initialize(DalamudPluginInterface pluginInterface) {
            this.pi = pluginInterface ?? throw new ArgumentNullException(nameof(pluginInterface), "DalamudPluginInterface cannot be null");
            
            this.config = this.pi.GetPluginConfig() as Configuration ?? new Configuration();
            this.config.Initialize(this.pi);

            this.ui = new PluginUI(this.pi, this.config);
            this.maps = new TreasureMaps(this.pi, this.config);

            this.pi.UiBuilder.OnBuildUi += this.ui.Draw;
            this.pi.UiBuilder.OnOpenConfigUi += this.ui.OpenSettings;
            this.pi.Framework.Gui.HoveredItemChanged += this.maps.OnHover;
            this.pi.Framework.Network.OnNetworkMessage += this.maps.OnNetwork;
            this.pi.CommandManager.AddHandler("/pglobetrotter", new CommandInfo(this.OnConfigCommand) {
                HelpMessage = "Show the Globetrotter config"
            });
            this.pi.CommandManager.AddHandler("/tmap", new CommandInfo(this.OnCommand) {
                HelpMessage = "Open the map and place a flag at the location of your current treasure map"
            });
        }

        private void OnConfigCommand(string command, string args) {
            this.ui.OpenSettings(null, null);
        }

        private void OnCommand(string command, string args) {
            this.maps.OpenMapLocation();
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    this.pi.UiBuilder.OnBuildUi -= this.ui.Draw;
                    this.pi.UiBuilder.OnOpenConfigUi -= this.ui.OpenSettings;
                    this.pi.Framework.Gui.HoveredItemChanged -= this.maps.OnHover;
                    this.pi.Framework.Network.OnNetworkMessage -= this.maps.OnNetwork;
                    this.pi.CommandManager.RemoveHandler("/pglobetrotter");
                    this.pi.CommandManager.RemoveHandler("/tmap");
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~GlobetrotterPlugin()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
