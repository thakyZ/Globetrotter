using ImGuiNET;
using System;

namespace Globetrotter {
    internal class PluginUi {
        private Configuration Config { get; }

        private bool _displaySettings;

        private bool DisplaySettings {
            get => this._displaySettings;
            set => this._displaySettings = value;
        }

        public PluginUi(Configuration config) {
            this.Config = config ?? throw new ArgumentNullException(nameof(config), "Configuration cannot be null");
        }

        public void OpenSettings(object sender, EventArgs e) {
            this.DisplaySettings = true;
        }

        public void Draw() {
            if (!this.DisplaySettings) {
                return;
            }

            if (!ImGui.Begin("Globetrotter settings", ref this._displaySettings)) {
                return;
            }

            ImGui.TextUnformatted("Use /tmap to open your current treasure map.");
            ImGui.TextUnformatted("If you have a map and this plugin isn't working, change zone.");

            ImGui.Separator();

            var showOnHover = this.Config.ShowOnHover;
            if (ImGui.Checkbox("Show on hover", ref showOnHover)) {
                this.Config.ShowOnHover = showOnHover;
                this.Config.Save();
            }

            var showOnOpen = this.Config.ShowOnOpen;
            if (ImGui.Checkbox("Show on open", ref showOnOpen)) {
                this.Config.ShowOnOpen = showOnOpen;
                this.Config.Save();
            }

            ImGui.End();
        }
    }
}
