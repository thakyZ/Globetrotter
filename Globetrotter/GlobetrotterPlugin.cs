using Dalamud.Game.Command;
using Dalamud.Plugin;
using System;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Game.Text;
using Dalamud.Utility;
using XivCommon;
using System.Linq;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using ImGuiNET;

namespace Globetrotter {
    // ReSharper disable once ClassNeverInstantiated.Global
    public class GlobetrotterPlugin : IDalamudPlugin {
        private bool _disposedValue;

        public string Name => "Globetrotter";

        [PluginService]
        [AllowNull, NotNull]
        private DalamudPluginInterface Interface { get; set; }

        [PluginService]
        [AllowNull, NotNull]
        private CommandManager CommandManager { get; set; }

        [PluginService]
        [AllowNull, NotNull]
        internal DataManager DataManager { get; set; }

        [PluginService]
        [AllowNull, NotNull]
        internal GameGui GameGui { get; set; }

        [PluginService]
        [AllowNull, NotNull]
        internal SigScanner SigScanner { get; set; }

        [PluginService]
        [AllowNull, NotNull]
        internal ChatGui ChatGui { get; set; }

        internal Configuration Config { get; }
        internal ChatTwoIntegration ChatTwoIntegration { get; }
        internal XivCommonBase XivCommon { get; }
        private PluginUi Ui { get; }
        private TreasureMaps Maps { get; }

        public GlobetrotterPlugin() {
            this.Config = this.Interface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Config.Initialize(this.Interface);

            this.Ui = new PluginUi(this);
            this.Maps = new TreasureMaps(this, this.Interface);
            this.XivCommon = new XivCommonBase(Hooks.None);
            this.ChatTwoIntegration = new ChatTwoIntegration(this.Interface, this.Maps);

            this.Interface.UiBuilder.Draw += this.Ui.Draw;
            this.Interface.UiBuilder.OpenConfigUi += this.Ui.OpenSettings;
            this.GameGui.HoveredItemChanged += this.Maps.OnHover;
            this.CommandManager.AddHandler("/pglobetrotter", new CommandInfo(this.OnConfigCommand) {
                HelpMessage = "Show the Globetrotter config",
            });
            this.CommandManager.AddHandler("/tmap", new CommandInfo(this.OnCommand) {
                HelpMessage = "Open the map and place a flag at the location of your current treasure map",
            });
        }

        private void OnConfigCommand(string command, string args) {
            this.Ui.OpenSettings();
        }

        private void OnCommand(string command, string args) {
            //this.Maps.OpenMapLocation();
            var link = false;
            var echo = false;
            if (!args.IsNullOrEmpty()) { 
                string[] multiArgs = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                link = multiArgs.Any(x => x.ToLower().Equals("link", StringComparison.InvariantCultureIgnoreCase)
                                                     || x.ToLower().Equals("l", StringComparison.InvariantCultureIgnoreCase));
                echo = multiArgs.Any(x => x.ToLower().Equals("echo", StringComparison.InvariantCultureIgnoreCase)
                                                     || x.ToLower().Equals("e", StringComparison.InvariantCultureIgnoreCase));
            }
            this.Maps.OpenMapLocation(link, echo);
        }

        protected virtual void Dispose(bool disposing) {
            if (this._disposedValue) {
                return;
            }

            if (disposing) {
                this.Interface.UiBuilder.Draw -= this.Ui.Draw;
                this.Interface.UiBuilder.OpenConfigUi -= this.Ui.OpenSettings;
                this.GameGui.HoveredItemChanged -= this.Maps.OnHover;
                this.Maps.Dispose();
                this.CommandManager.RemoveHandler("/pglobetrotter");
                this.CommandManager.RemoveHandler("/tmap");
                this.ChatTwoIntegration.Dispose();
            }

            this._disposedValue = true;
        }

        public void PrintChat(XivChatEntry msg) {
            this.ChatGui.PrintChat(msg);
        }

        public void PrintChat(string msg) {
            this.ChatGui.Print(msg);
        }

        internal static void CopyToClipboard(string message) => ImGui.SetClipboardText(message);

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
