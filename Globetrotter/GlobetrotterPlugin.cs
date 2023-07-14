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
using ImGuiNET;

namespace Globetrotter {
    // ReSharper disable once ClassNeverInstantiated.Global
    public class GlobetrotterPlugin : IDalamudPlugin {
        private bool _disposedValue;

        public string Name => "Globetrotter";
    
        [PluginService]
        [RequiredVersion("1.0")]
        internal DalamudPluginInterface Interface { get; init; }

        [PluginService]
        [RequiredVersion("1.0")]
        private CommandManager CommandManager { get; init; }

        [PluginService]
        [RequiredVersion("1.0")]
        internal DataManager DataManager { get; init; }

        [PluginService]
        [RequiredVersion("1.0")]
        internal GameGui GameGui { get; init; }

        [PluginService]
        [RequiredVersion("1.0")]
        internal SigScanner SigScanner { get; init; }

        [PluginService]
        [RequiredVersion("1.0")]
        internal ChatGui ChatGui { get; init; }

        internal Configuration Config { get; }
        internal ChatTwoIntegration ChatTwoIntegration { get; }
        internal XivCommonBase XivCommon { get; }
        private PluginUi Ui { get; }
        internal TreasureMaps Maps { get; }

        public GlobetrotterPlugin([RequiredVersion("1.0")] DalamudPluginInterface _interface,
          [RequiredVersion("1.0")] GameGui gameGui,
          [RequiredVersion("1.0")] SigScanner sigScanner,
          [RequiredVersion("1.0")] DataManager dataManager,
          [RequiredVersion("1.0")] CommandManager commandManager,
          [RequiredVersion("1.0")] ChatGui chatGui) {
            this.Config = _interface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Config.Initialize(_interface);
            this.Interface = _interface;
            this.CommandManager = commandManager;
            this.GameGui = gameGui;
            this.ChatGui = chatGui;
            this.SigScanner = sigScanner;
            this.DataManager = dataManager;

            this.Ui = new PluginUi(this);
            this.Maps = new TreasureMaps(this);
            this.XivCommon = new XivCommonBase(Hooks.None);
            this.ChatTwoIntegration = new ChatTwoIntegration(this);

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
