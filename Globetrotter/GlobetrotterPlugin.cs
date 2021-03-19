using Dalamud.Game.Command;
using Dalamud.Plugin;
using System;

namespace Globetrotter {
    public class GlobetrotterPlugin : IDalamudPlugin {
        private bool _disposedValue;

        public string Name => "Globetrotter";

        private DalamudPluginInterface _pi = null!;
        private Configuration _config = null!;
        private PluginUi _ui = null!;
        private TreasureMaps _maps = null!;

        public void Initialize(DalamudPluginInterface pluginInterface) {
            this._pi = pluginInterface ?? throw new ArgumentNullException(nameof(pluginInterface), "DalamudPluginInterface cannot be null");

            this._config = this._pi.GetPluginConfig() as Configuration ?? new Configuration();
            this._config.Initialize(this._pi);

            this._ui = new PluginUi(this._config);
            this._maps = new TreasureMaps(this._pi, this._config);

            this._pi.UiBuilder.OnBuildUi += this._ui.Draw;
            this._pi.UiBuilder.OnOpenConfigUi += this._ui.OpenSettings;
            this._pi.Framework.Gui.HoveredItemChanged += this._maps.OnHover;
            this._pi.CommandManager.AddHandler("/pglobetrotter", new CommandInfo(this.OnConfigCommand) {
                HelpMessage = "Show the Globetrotter config",
            });
            this._pi.CommandManager.AddHandler("/tmap", new CommandInfo(this.OnCommand) {
                HelpMessage = "Open the map and place a flag at the location of your current treasure map",
            });
        }

        private void OnConfigCommand(string command, string args) {
            this._ui.OpenSettings(null, null);
        }

        private void OnCommand(string command, string args) {
            this._maps.OpenMapLocation();
        }

        protected virtual void Dispose(bool disposing) {
            if (this._disposedValue) {
                return;
            }

            if (disposing) {
                this._pi.UiBuilder.OnBuildUi -= this._ui.Draw;
                this._pi.UiBuilder.OnOpenConfigUi -= this._ui.OpenSettings;
                this._pi.Framework.Gui.HoveredItemChanged -= this._maps.OnHover;
                this._maps.Dispose();
                this._pi.CommandManager.RemoveHandler("/pglobetrotter");
                this._pi.CommandManager.RemoveHandler("/tmap");
            }

            this._disposedValue = true;
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
