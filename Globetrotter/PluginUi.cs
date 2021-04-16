using ImGuiNET;
using System;
using System.Numerics;

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

            ImGui.SetNextWindowSize(new Vector2(350, 250), ImGuiCond.FirstUseEver);

            if (!ImGui.Begin("Globetrotter settings", ref this._displaySettings)) {
                ImGui.End();
                return;
            }

            ImGui.TextUnformatted("Use /tmap to open your current treasure map.");

            ImGui.Separator();

            var showOnDecipher = this.Config.ShowOnDecipher;
            if (HelpCheckbox("Show on decipher", "Open the map with a flag set after deciphering a map.", ref showOnDecipher)) {
                this.Config.ShowOnDecipher = showOnDecipher;
                this.Config.Save();
            }

            ImGui.Separator();

            var showOnOpen = this.Config.ShowOnOpen;
            if (HelpCheckbox("Show on open", "Open the map with a flag set instead of the normal treasure map window.", ref showOnOpen)) {
                this.Config.ShowOnOpen = showOnOpen;
                this.Config.Save();
            }

            ImGui.Separator();

            var showOnHover = this.Config.ShowOnHover;
            if (HelpCheckbox("Show on hover", "Open the map with a flag set when hovering over a deciphered map.", ref showOnHover)) {
                this.Config.ShowOnHover = showOnHover;
                this.Config.Save();
            }

            ImGui.End();
        }

        private static bool HelpCheckbox(string label, string help, ref bool isChecked) {
            var ret = ImGui.Checkbox(label, ref isChecked);

            ImGui.TreePush();
            ImGui.PushTextWrapPos();
            ImGui.TextUnformatted(help);
            ImGui.PopTextWrapPos();
            ImGui.TreePop();

            return ret;
        }
    }
}
