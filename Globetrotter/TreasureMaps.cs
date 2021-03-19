using Dalamud.Game.Chat.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Globetrotter {
    internal sealed class TreasureMaps : IDisposable {
        private const uint TreasureMapsCode = 0x54;

        private static Dictionary<uint, uint> _mapToRow;

        private Dictionary<uint, uint> MapToRow {
            get {
                if (_mapToRow != null) {
                    return _mapToRow;
                }

                var mapToRow = new Dictionary<uint, uint>();

                foreach (var rank in this.Interface.Data.GetExcelSheet<TreasureHuntRank>()) {
                    var unopened = rank.ItemName.Value;
                    if (unopened == null) {
                        continue;
                    }

                    EventItem opened;
                    // FIXME: remove this try/catch when lumina is fixed
                    try {
                        opened = rank.KeyItemName.Value;
                    } catch (NullReferenceException) {
                        opened = null;
                    }

                    if (opened == null) {
                        continue;
                    }

                    mapToRow[opened.RowId] = unopened.AdditionalData;
                }

                _mapToRow = mapToRow;

                return _mapToRow;
            }
        }

        private DalamudPluginInterface Interface { get; }
        private Configuration Config { get; }
        private TreasureMapPacket _lastMap;

        private delegate char HandleActorControlSelfDelegate(long a1, long a2, IntPtr dataPtr);

        private readonly Hook<HandleActorControlSelfDelegate> _acsHook;

        public TreasureMaps(DalamudPluginInterface pi, Configuration config) {
            this.Interface = pi ?? throw new ArgumentNullException(nameof(pi), "DalamudPluginInterface cannot be null");
            this.Config = config ?? throw new ArgumentNullException(nameof(config), "Configuration cannot be null");

            var delegatePtr = this.Interface.TargetModuleScanner.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 48 8B D9 49 8B F8 41 0F B7 08");
            if (delegatePtr == IntPtr.Zero) {
                PluginLog.Log("Unable to detect treasure maps because could not find ACS handler delegate");
                return;
            }

            this._acsHook = new Hook<HandleActorControlSelfDelegate>(delegatePtr, new HandleActorControlSelfDelegate(this.OnACS));
            this._acsHook.Enable();
        }

        public void OnHover(object sender, ulong id) {
            if (!this.Config.ShowOnHover || this._lastMap == null || this._lastMap.EventItemId != id) {
                return;
            }

            this.OpenMapLocation();
        }

        private char OnACS(long a1, long a2, IntPtr dataPtr) {
            var packet = ParsePacket(dataPtr);
            if (packet == null) {
                return this._acsHook.Original(a1, a2, dataPtr);
            }

            this._lastMap = packet;

            if (this.Config.ShowOnOpen && packet.JustOpened) {
                // this does not work because the offset in memory is not yet updated with the thing
                this.OpenMapLocation();
            }

            return this._acsHook.Original(a1, a2, dataPtr);
        }

        public void OpenMapLocation() {
            var packet = this._lastMap;

            if (packet == null) {
                return;
            }

            if (!this.MapToRow.TryGetValue(packet.EventItemId, out var rowId)) {
                return;
            }

            var spot = this.Interface.Data.GetExcelSheet<TreasureSpot>().GetRow(rowId, packet.SubRowId);

            var loc = spot?.Location?.Value;
            var map = loc?.Map?.Value;
            var terr = map?.TerritoryType?.Value;

            if (terr == null) {
                return;
            }

            var x = ToMapCoordinate(loc.X, map.SizeFactor);
            var y = ToMapCoordinate(loc.Z, map.SizeFactor);
            var mapLink = new MapLinkPayload(
                this.Interface.Data,
                terr.RowId,
                map.RowId,
                ConvertMapCoordinateToRawPosition(x, map.SizeFactor),
                ConvertMapCoordinateToRawPosition(y, map.SizeFactor)
            );

            this.Interface.Framework.Gui.OpenMapWithMapLink(mapLink);
        }

        private static TreasureMapPacket ParsePacket(IntPtr dataPtr) {
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

            return new TreasureMapPacket(eventItemId, subRowId, justOpened);
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

        public void Dispose() {
            this._acsHook?.Dispose();
        }
    }

    internal class TreasureMapPacket {
        public uint EventItemId { get; }
        public uint SubRowId { get; }
        public bool JustOpened { get; }

        public TreasureMapPacket(uint eventItemId, uint subRowId, bool justOpened) {
            this.EventItemId = eventItemId;
            this.SubRowId = subRowId;
            this.JustOpened = justOpened;
        }
    }
}
