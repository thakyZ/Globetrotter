using Dalamud.Plugin;
using ImGuiNET;
using System;

namespace Globetrotter {
    class PluginUI {
        private readonly DalamudPluginInterface pi;
        private readonly Configuration config;

        private bool _displaySettings = false;
        internal bool DisplaySettings { get => this._displaySettings; private set => this._displaySettings = value; }

        public PluginUI(DalamudPluginInterface pi, Configuration config) {
            this.pi = pi ?? throw new ArgumentNullException(nameof(pi), "DalamudPluginInterface cannot be null");
            this.config = config ?? throw new ArgumentNullException(nameof(config), "Configuration cannot be null");
        }

        public void OpenSettings(object sender, EventArgs e) {
            this.DisplaySettings = true;
        }

        public void Draw() {
            if (!this.DisplaySettings) {
                return;
            }

            if (ImGui.Begin("Globetrotter settings", ref this._displaySettings)) {
                ImGui.Text("Use /tmap to open your current treasure map.");
                ImGui.Text("If you have a map and this plugin isn't working, change zone.");

                ImGui.Separator();

                bool showOnHover = this.config.ShowOnHover;
                if (ImGui.Checkbox("Show on hover", ref showOnHover)) {
                    this.config.ShowOnHover = showOnHover;
                    this.config.Save();
                }

                bool showOnOpen = this.config.ShowOnOpen;
                if (ImGui.Checkbox("Show on open", ref showOnOpen)) {
                    this.config.ShowOnOpen = showOnOpen;
                    this.config.Save();
                }

                ImGui.End();
            }
        }
    }
}
