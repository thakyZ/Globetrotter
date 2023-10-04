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
using Dalamud.Plugin.Services;

namespace Globetrotter {
    // ReSharper disable once ClassNeverInstantiated.Global
    public class GlobetrotterPlugin : IDalamudPlugin {
        private bool _disposedValue;

        public string Name => "Globetrotter";

        [PluginService]
        [AllowNull, NotNull]
        internal static DalamudPluginInterface Interface { get; set; }

        [PluginService]
        [AllowNull, NotNull]
        private static ICommandManager CommandManager { get; set; }

        [PluginService]
        [AllowNull, NotNull]
        internal static IDataManager DataManager { get; set; }

        [PluginService]
        [AllowNull, NotNull]
        internal static IGameGui GameGui { get; set; }

        [PluginService]
        [AllowNull, NotNull]
        internal static SigScanner SigScanner { get; set; }

        [PluginService]
        [AllowNull, NotNull]
        internal static IChatGui ChatGui { get; set; }

        [PluginService]
        [AllowNull, NotNull]
        internal static IPluginLog PluginLog { get; set; }

        [PluginService]
        [AllowNull, NotNull]
        internal static IGameInteropProvider GameInteropProvider { get; set; }

        internal Configuration Config { get; }
        internal ChatTwoIntegration ChatTwoIntegration { get; }
        internal XivCommonBase XivCommon { get; }
        private PluginUi Ui { get; }
        private TreasureMaps Maps { get; }

        public GlobetrotterPlugin() {
            this.Config = Interface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Config.Initialize(Interface);

            this.Ui = new PluginUi(this);
            this.Maps = new TreasureMaps(this);
            this.XivCommon = new XivCommonBase(Hooks.None);
            this.ChatTwoIntegration = new ChatTwoIntegration(this.Maps);

            Interface.UiBuilder.Draw += this.Ui.Draw;
            Interface.UiBuilder.OpenConfigUi += this.Ui.OpenSettings;
            GameGui.HoveredItemChanged += this.Maps.OnHover;
            CommandManager.AddHandler("/pglobetrotter", new CommandInfo(this.OnConfigCommand) {
                HelpMessage = "Show the Globetrotter config",
            });
            CommandManager.AddHandler("/tmap", new CommandInfo(this.OnCommand) {
                HelpMessage = "Open the map and place a flag at the location of your current treasure map",
            });
        }

        private void OnConfigCommand(string command, string args) {
            this.Ui.OpenSettings();
        }

        private void OnCommand(string command, string args) {
            // this.Maps.OpenMapLocation();
            var link = false;
            var echo = false;
            if (!args.IsNullOrEmpty()) { 
                string[] multiArgs = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                link = Array.Exists(multiArgs, x => x.ToLower().Equals("link", StringComparison.InvariantCultureIgnoreCase)
                                                     || x.ToLower().Equals("l", StringComparison.InvariantCultureIgnoreCase));
                echo = Array.Exists(multiArgs, x => x.ToLower().Equals("echo", StringComparison.InvariantCultureIgnoreCase)
                                                     || x.ToLower().Equals("e", StringComparison.InvariantCultureIgnoreCase));
            }
            this.Maps.OpenMapLocation(link, echo);
        }

        protected virtual void Dispose(bool disposing) {
            if (this._disposedValue) {
                return;
            }

            if (disposing) {
                Interface.UiBuilder.Draw -= this.Ui.Draw;
                Interface.UiBuilder.OpenConfigUi -= this.Ui.OpenSettings;
                GameGui.HoveredItemChanged -= this.Maps.OnHover;
                this.Maps.Dispose();
                CommandManager.RemoveHandler("/pglobetrotter");
                CommandManager.RemoveHandler("/tmap");
                this.ChatTwoIntegration.Dispose();
            }

            this._disposedValue = true;
        }

        internal static void CopyToClipboard(string message) => ImGui.SetClipboardText(message);

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
