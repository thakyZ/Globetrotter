using Dalamud.Game.Chat.SeStringHandling.Payloads;
using Dalamud.Game.Internal.Network;
using Dalamud.Hooking;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Globetrotter {
    class TreasureMaps : IDisposable {
        private const uint TREASURE_MAPS = 0x54;

        private static Dictionary<uint, uint> _mapToRow;
        private Dictionary<uint, uint> MapToRow {
            get {
                if (_mapToRow != null) {
                    return _mapToRow;
                }

                Dictionary<uint, uint> mapToRow = new Dictionary<uint, uint>();

                foreach (TreasureHuntRank rank in this.pi.Data.GetExcelSheet<TreasureHuntRank>()) {
                    Item unopened = rank.ItemName.Value;
                    if (unopened == null) {
                        continue;
                    }

                    EventItem opened = rank.KeyItemName.Value;
                    if (opened == null) {
                        continue;
                    }

                    mapToRow[opened.RowId] = unopened.AdditionalData;
                }

                _mapToRow = mapToRow;

                return _mapToRow;
            }
        }

        private readonly DalamudPluginInterface pi;
        private readonly Configuration config;
        private TreasureMapPacket lastMap;
        private bool disposedValue;

        private delegate char HandleActorControlSelfDelegate(long a1, long a2, IntPtr dataPtr);
        private readonly Hook<HandleActorControlSelfDelegate> acsHook;

        public TreasureMaps(DalamudPluginInterface pi, Configuration config) {
            this.pi = pi ?? throw new ArgumentNullException(nameof(pi), "DalamudPluginInterface cannot be null");
            this.config = config ?? throw new ArgumentNullException(nameof(config), "Configuration cannot be null");

            IntPtr delegatePtr = this.pi.TargetModuleScanner.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 48 8B D9 49 8B F8 41 0F B7 08");
            if (delegatePtr == IntPtr.Zero) {
                PluginLog.Log("Unable to detect treasure maps because could not find ACS handler delegate");
                return;
            }

            this.acsHook = new Hook<HandleActorControlSelfDelegate>(delegatePtr, new HandleActorControlSelfDelegate(this.OnACS));
            this.acsHook.Enable();
        }

        public void OnHover(object sender, ulong id) {
            if (!this.config.ShowOnHover || this.lastMap == null || this.lastMap.EventItemId != id) {
                return;
            }

            this.OpenMapLocation();
        }

        private char OnACS(long a1, long a2, IntPtr dataPtr) {
            TreasureMapPacket packet = ParsePacket(dataPtr);
            if (packet == null) {
                return this.acsHook.Original(a1, a2, dataPtr);
            }

            this.lastMap = packet;

            if (this.config.ShowOnOpen && packet.JustOpened) {
                // this does not work because the offset in memory is not yet updated with the thing
                this.OpenMapLocation();
            }

            return this.acsHook.Original(a1, a2, dataPtr);
        }

        public void OpenMapLocation() {
            TreasureMapPacket packet = this.lastMap;

            if (packet == null) {
                return;
            }

            if (!this.MapToRow.TryGetValue(packet.EventItemId, out uint rowId)) {
                return;
            }

            TreasureSpot spot = this.pi.Data.GetExcelSheet<TreasureSpot>().GetRow(rowId, packet.SubRowId);
            if (spot == null) {
                return;
            }

            if (spot.Location.Value == null) {
                return;
            }
            Level loc = spot.Location.Value;

            if (loc.Map.Value == null) {
                return;
            }
            Map map = loc.Map.Value;

            if (map.TerritoryType.Value == null) {
                return;
            }
            TerritoryType terr = map.TerritoryType.Value;

            float x = ToMapCoordinate(loc.X, map.SizeFactor);
            float y = ToMapCoordinate(loc.Z, map.SizeFactor);
            MapLinkPayload mapLink = new MapLinkPayload(
                this.pi.Data,
                terr.RowId,
                map.RowId,
                ConvertMapCoordinateToRawPosition(x, map.SizeFactor),
                ConvertMapCoordinateToRawPosition(y, map.SizeFactor)
            );

            this.pi.Framework.Gui.OpenMapWithMapLink(mapLink);
        }

        public static TreasureMapPacket ParsePacket(IntPtr dataPtr) {
            uint category = (uint)Marshal.ReadByte(dataPtr);
            if (category != TREASURE_MAPS) {
                return null;
            }

            dataPtr += 4; // skip padding
            uint param1 = (uint)Marshal.ReadInt32(dataPtr);
            dataPtr += 4;
            uint param2 = (uint)Marshal.ReadInt32(dataPtr);
            dataPtr += 4;
            uint param3 = (uint)Marshal.ReadInt32(dataPtr);

            uint eventItemId = param1;
            uint subRowId = param2;
            bool justOpened = param3 == 1;

            return new TreasureMapPacket(eventItemId, subRowId, justOpened);
        }

        private static int ConvertMapCoordinateToRawPosition(float pos, float scale) {
            var c = scale / 100.0f;

            var scaledPos = (((pos - 1.0f) * c / 41.0f * 2048.0f) - 1024.0f) / c;
            scaledPos *= 1000.0f;

            return (int)scaledPos;
        }
        private static float ToMapCoordinate(float val, float scale) {
            var c = scale / 100f;

            val *= c;
            return (41f / c * ((val + 1024f) / 2048f)) + 1;
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    this.acsHook?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    class TreasureMapPacket {
        public uint EventItemId { get; private set; }
        public uint SubRowId { get; private set; }
        public bool JustOpened { get; private set; }

        public TreasureMapPacket(uint eventItemId, uint subRowId, bool justOpened) {
            this.EventItemId = eventItemId;
            this.SubRowId = subRowId;
            this.JustOpened = justOpened;
        }
    }
}
