using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;

using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Globetrotter {
    internal unsafe class GameIntegration : IDisposable {
        private GlobetrotterPlugin Plugin { get; }

        private delegate byte InsertTextCommandDelegate(AgentInterface* agent, uint paramID, byte a3 = 0);
        private InsertTextCommandDelegate insertTextCommand;

        private AgentInterface* ChatAgent => Framework.Instance()->UIModule->GetAgentModule()->GetAgentByInternalId(AgentId.ChatLog);
        private AgentInterface* MapAgent => Framework.Instance()->UIModule->GetAgentModule()->GetAgentByInternalId(AgentId.Map);
    
        internal GameIntegration(GlobetrotterPlugin plugin) {
            this.Plugin = plugin;
            PluginLog.Verbose("Created Game Integration");
            if (insertTextCommand is null) {
                var insertTextCommandPtr = this.Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 40 88 6E 08 EB 04");
                insertTextCommand = Marshal.GetDelegateForFunctionPointer<InsertTextCommandDelegate>(insertTextCommandPtr);
            }
        }

        public void InsertFlagInChat() {
            PluginLog.Information($"insertFlagTextCommand is {(insertTextCommand is null ? "null" : "not null")}");
            if (insertTextCommand is not null) {
                insertTextCommand(ChatAgent, 1048u);
            }
        }
    
        public void Dispose()
        {
        }
    }
}
