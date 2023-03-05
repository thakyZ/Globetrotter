using Dalamud.Game.Command;
using Dalamud.Plugin;
using System;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Game.Text;
using Dalamud.Utility;

namespace Globetrotter {
    // ReSharper disable once ClassNeverInstantiated.Global
    public class GlobetrotterPlugin : IDalamudPlugin {
        private bool _disposedValue;

        public string Name => "Globetrotter";

        [PluginService]
        private DalamudPluginInterface Interface { get; init; } = null!;

        [PluginService]
        private CommandManager CommandManager { get; init; } = null!;

        [PluginService]
        internal DataManager DataManager { get; init; } = null!;

        [PluginService]
        internal GameGui GameGui { get; init; } = null!;

        [PluginService]
        internal SigScanner SigScanner { get; init; } = null!;

        [PluginService]
        public ChatGui ChatGui { get; init; } = null!;

        internal Configuration Config { get; }
        internal GameIntegration GameIntegration { get; }
        private PluginUi Ui { get; }
        private TreasureMaps Maps { get; }

        public GlobetrotterPlugin() {
            this.Config = this.Interface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Config.Initialize(this.Interface);

            this.Ui = new PluginUi(this);
            this.Maps = new TreasureMaps(this);
            this.GameIntegration = new GameIntegration(this);

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
                link = multiArgs[0].ToLower() == "link";
                echo = multiArgs.Length == 2 && (multiArgs[1].ToLower() == "e" || multiArgs[1].ToLower() == "echo");
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
            }

            this._disposedValue = true;
        }

        public void PrintChat(XivChatEntry msg)
        {
            ChatGui.PrintChat(msg);
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
