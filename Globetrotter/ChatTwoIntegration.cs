using System;

using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

using ImGuiNET;

namespace Globetrotter {
    internal class ChatTwoIntegration : IDisposable {
        private TreasureMaps Maps { get; }

        private ICallGateSubscriber<string> Register { get; }
        private ICallGateSubscriber<string, object?> Unregister { get; }
        private ICallGateSubscriber<object?> Available { get; }
        private ICallGateSubscriber<string, PlayerPayload?, ulong, Payload?, SeString?, SeString?, object?> Invoke { get; }

        private string? _id;

        internal ChatTwoIntegration(TreasureMaps maps) {
            this.Maps = maps;

            this.Register = GlobetrotterPlugin.Interface.GetIpcSubscriber<string>("ChatTwo.Register");
            this.Unregister = GlobetrotterPlugin.Interface.GetIpcSubscriber<string, object?>("ChatTwo.Unregister");
            this.Invoke = GlobetrotterPlugin.Interface.GetIpcSubscriber<string, PlayerPayload?, ulong, Payload?, SeString?, SeString?, object?>("ChatTwo.Invoke");
            this.Available = GlobetrotterPlugin.Interface.GetIpcSubscriber<object?>("ChatTwo.Available");

            this.Available.Subscribe(this.DoRegister);
            try {
                this.DoRegister();
            } catch (Exception) {
                // try to register if chat 2 is already loaded
                // if not, just ignore exception
            }

            this.Invoke.Subscribe(this.Integration);
        }

        public void Dispose() {
            if (this._id != null) {
                try {
                    this.Unregister.InvokeAction(this._id);
                } catch (Exception) {
                    // no-op
                }

                this._id = null;
            }

            this.Invoke.Unsubscribe(this.Integration);
            this.Available.Unsubscribe(this.DoRegister);
        }

        private void DoRegister() {
            this._id = this.Register.InvokeFunc();
        }

        private void Integration(string id, Payload? sender, ulong contentId, Payload? payload, SeString? senderString, SeString? content) {
            if (id != this._id) {
                return;
            }

            if (payload is null) {
                return;
            }

            if (ImGui.Selectable("Link Opened Map and Copy to Clipboard")) {
                this.Maps.OpenMapLocation(true, false);
            }
        }
    }
}