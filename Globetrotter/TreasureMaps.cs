using Dalamud.Hooking;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using System.Net.Sockets;
using Dalamud.Utility;
using static FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using static System.Net.Mime.MediaTypeNames;
using System.Text;
using Dalamud.Plugin;

namespace Globetrotter {
    internal sealed class TreasureMaps : IDisposable {

        private const uint TreasureMapsCode = 0x54;

        private static Dictionary<uint, uint>? _mapToRow;

        private Dictionary<uint, uint> MapToRow {
            get {
                if (_mapToRow != null) {
                    return _mapToRow;
                }

                var mapToRow = new Dictionary<uint, uint>();

                foreach (var rank in this.Plugin.DataManager.GetExcelSheet<TreasureHuntRank>()!) {
                    var unopened = rank.ItemName.Value;
                    if (unopened == null) {
                        continue;
                    }

                    EventItem? opened;
                    // FIXME: remove this try/catch when lumina is fixed
                    try {
                        opened = rank.KeyItemName.Value;
                    } catch (NullReferenceException) {
                        opened = null;
                    }

                    if (opened == null) {
                        continue;
                    }

                    mapToRow[opened.RowId] = rank.RowId;
                }

                _mapToRow = mapToRow;

                return _mapToRow;
            }
        }

        private GlobetrotterPlugin Plugin { get; }
        private DalamudPluginInterface PluginInterface { get; }
        private TreasureMapPacket? _lastMap;

        /// <summary>
        /// Gets the _lastMap EventId
        /// <para>
        /// If the _lastMap object is null it will return the event id
        /// of a Archaeoskin Treasure Map as a backup and a safety
        /// precaution.
        /// </para>
        /// </summary>
        private uint LastMapEventId => _lastMap?.EventItemId ?? 2001762;

        private delegate char HandleActorControlSelfDelegate(long a1, long a2, IntPtr dataPtr);

        private delegate IntPtr ShowTreasureMapDelegate(IntPtr manager, ushort rowId, ushort subRowId, byte a4);

        private readonly Hook<HandleActorControlSelfDelegate> _acsHook;
        private readonly Hook<ShowTreasureMapDelegate> _showMapHook;


        private List<uint> UsedPayloadIds { get; } = new();

        public TreasureMaps(GlobetrotterPlugin plugin, DalamudPluginInterface pluginInterface) {
            this.Plugin = plugin;
            this.PluginInterface = pluginInterface;

            var acsPtr = this.Plugin.SigScanner.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 48 8B D9 49 8B F8 41 0F B7 08");
            this._acsHook = Hook<HandleActorControlSelfDelegate>.FromAddress(acsPtr, this.OnACS);
            this._acsHook.Enable();

            var showMapPtr = this.Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 40 84 FF 0F 85 ?? ?? ?? ?? 48 8B 0D");
            this._showMapHook = Hook<ShowTreasureMapDelegate>.FromAddress(showMapPtr, this.OnShowMap);
            this._showMapHook.Enable();
        }

        public void Dispose() {
            this._acsHook.Dispose();
            this._showMapHook.Dispose();
        }

        public void OnHover(object? sender, ulong id) {
            if (!this.Plugin.Config.ShowOnHover || this._lastMap == null || this._lastMap.EventItemId != id) {
                return;
            }

            this.OpenMapLocation();
        }

        private IntPtr OnShowMap(IntPtr manager, ushort rowId, ushort subRowId, byte a4) {
            try {
                if (!this.OnShowMapInner(rowId, subRowId)) {
                    return IntPtr.Zero;
                }
            } catch (Exception ex) {
                PluginLog.LogError(ex, "Exception on show map");
            }

            return this._showMapHook.Original(manager, rowId, subRowId, a4);
        }

        private bool OnShowMapInner(ushort rowId, ushort subRowId) {
            if (this._lastMap == null) {
                try {
                    var eventItemId = this.MapToRow.First(entry => entry.Value == rowId);
                    this._lastMap = new TreasureMapPacket(eventItemId.Key, subRowId, false, ToMapName(eventItemId.Key));
                } catch (InvalidOperationException) {
                    // no-op
                }
            }

            if (!this.Plugin.Config.ShowOnOpen && (!this.Plugin.Config.ShowOnDecipher || this._lastMap?.JustOpened != true)) {
                return true;
            }

            this.OpenMapLocation();
            return false;
        }

        private char OnACS(long a1, long a2, IntPtr dataPtr) {
            try {
                this.OnACSInner(dataPtr);
            } catch (Exception ex) {
                PluginLog.LogError(ex, "Exception on ACS");
            }

            return this._acsHook.Original(a1, a2, dataPtr);
        }

        private void OnACSInner(IntPtr dataPtr) {
            var packet = ParsePacket(dataPtr);
            if (packet == null) {
                return;
            }

            this._lastMap = packet;
        }

        public MapLinkPayload? GetMapLinkPayload(out TerritoryType? terr, out Map? map, out float x, out float y) {
            x = y = 0.0f;
            terr = null;
            map = null;
            var packet = this._lastMap;

            if (packet == null) {
                return null;
            }

            if (!this.MapToRow.TryGetValue(packet.EventItemId, out var rowId)) {
                return null;
            }

            var spot = this.Plugin.DataManager.GetExcelSheet<TreasureSpot>()!.GetRow(rowId, packet.SubRowId);

            var loc = spot?.Location?.Value;
            map = loc?.Map?.Value;
            terr = map?.TerritoryType?.Value;

            if (terr == null) {
                return null;
            }

            x = ToMapCoordinate(loc!.X, map!.SizeFactor);
            y = ToMapCoordinate(loc.Z, map.SizeFactor);
            return new MapLinkPayload(
                terr.RowId,
                map.RowId,
                ConvertMapCoordinateToRawPosition(x, map.SizeFactor),
                ConvertMapCoordinateToRawPosition(y, map.SizeFactor)
            );
        }

        public MapLinkPayload? GetMapLinkPayload() {
            return this.GetMapLinkPayload(out _, out _, out _, out _);
        }

        public SeStringBuilder DrawMapName(SeStringBuilder seStringBuilder) {
            var mapName = _lastMap?.TreasureMapName ?? "Unknown";
            var isSpecial = mapName.EndsWith("special treasure map", StringComparison.CurrentCultureIgnoreCase);

            seStringBuilder = isSpecial ? seStringBuilder.AddUiGlow(578) : seStringBuilder.AddUiForeground(575);

            seStringBuilder = seStringBuilder.AddUiForeground(mapName.EndsWith("special treasure map", StringComparison.CurrentCultureIgnoreCase) ? (ushort)578 : (ushort)575);
            seStringBuilder = seStringBuilder.AddText(string.Format("{0}", _lastMap?.TreasureMapName ?? "Unknown"));

            seStringBuilder = isSpecial ? seStringBuilder.AddUiGlowOff() : seStringBuilder.AddUiForegroundOff();

            return seStringBuilder;
        }

        public DalamudLinkPayload CreatePayload(uint id) {
            if (UsedPayloadIds.Contains(id)) {
                UsedPayloadIds.RemoveAt(UsedPayloadIds.FindIndex(0, x => x == id));
                this.PluginInterface.RemoveChatLinkHandler(id);
            }
            UsedPayloadIds.Add(id);
            return this.PluginInterface.AddChatLinkHandler(id,
                    (i, m) => GlobetrotterPlugin.CopyToClipboard("My map is at <flag>"));
        }

        public void OpenMapLocation(bool link = false, bool echo = false) {
            var mapLink = GetMapLinkPayload(out TerritoryType? terr, out Map? map, out float x, out float y);

            if (mapLink == null || terr == null || map == null) {
                return;
            }

            this.Plugin.GameGui.OpenMapWithMapLink(mapLink);

            if (echo) {
                DalamudLinkPayload payload = CreatePayload(LastMapEventId);
                SeStringBuilder seStringBuilder = new SeStringBuilder();
                seStringBuilder = DrawMapName(seStringBuilder);
                seStringBuilder = seStringBuilder.AddText(" ");
                seStringBuilder = seStringBuilder.Append(SeString.CreateMapLink(terr.RowId, map.RowId, x, y, 0f));
                seStringBuilder = seStringBuilder.AddText(" ");
                seStringBuilder = seStringBuilder.AddUiForeground(32);
                seStringBuilder = seStringBuilder.Add(payload);
                seStringBuilder = seStringBuilder.AddText($"[Click to copy]");
                seStringBuilder = seStringBuilder.Add(RawPayload.LinkTerminator);
                seStringBuilder = seStringBuilder.AddUiForegroundOff();

                this.Plugin.PrintChat(new XivChatEntry
                {
                    Message = seStringBuilder.Build(),
                    Type = XivChatType.Debug
                });
                
            }
            if (link) {
                this.Plugin.PrintChat(new XivChatEntry
                {
                    Message = "Copied message to clipboard...",
                    Type = XivChatType.Debug
                });
                GlobetrotterPlugin.CopyToClipboard("My map is at <flag>");
            }

            if (this._lastMap != null) {
                this._lastMap.JustOpened = false;
            }
        }

        private TreasureMapPacket? ParsePacket(IntPtr dataPtr) {
            uint category = Marshal.ReadByte(dataPtr);
            if (category != TreasureMapsCode) {
                return null;
            }

            dataPtr += 4; // skip padding
            var param1 = (uint) Marshal.ReadInt32(dataPtr);
            dataPtr += 4;
            var param2 = (uint) Marshal.ReadInt32(dataPtr);
            dataPtr += 4;
            var param3 = (uint) Marshal.ReadInt32(dataPtr);

            var eventItemId = param1;
            var subRowId = param2;
            var justOpened = param3 == 1;

            return new TreasureMapPacket(eventItemId, subRowId, justOpened, ToMapName(eventItemId));
        }

        private static int ConvertMapCoordinateToRawPosition(float pos, float scale) {
            var c = scale / 100.0f;

            var scaledPos = (((pos - 1.0f) * c / 41.0f * 2048.0f) - 1024.0f) / c;
            scaledPos *= 1000.0f;

            return (int) scaledPos;
        }

        private static float ToMapCoordinate(float val, float scale) {
            var c = scale / 100f;

            val *= c;
            return (41f / c * ((val + 1024f) / 2048f)) + 1;
        }

        private string ToMapName(uint eventItemId) {
            var eventItem = this.Plugin.DataManager.GetExcelSheet<EventItem>()!.GetRow(eventItemId);
            return eventItem?.Name.ToDalamudString().TextValue ?? "Unknown";
        }
    }

    internal class TreasureMapPacket {
        public uint EventItemId { get; }
        public string TreasureMapName { get; }
        public uint SubRowId { get; }
        public bool JustOpened { get; set; }

        public TreasureMapPacket(uint eventItemId, uint subRowId, bool justOpened, string treasureMapName) {
            TreasureMapName = treasureMapName;
            this.EventItemId = eventItemId;
            this.SubRowId = subRowId;
            this.JustOpened = justOpened;
        }
    }
}
