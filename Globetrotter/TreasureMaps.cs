using Dalamud.Game.Chat.SeStringHandling.Payloads;
using Dalamud.Game.Internal.Network;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Globetrotter {
    class TreasureMaps {
        private static Dictionary<uint, uint> _mapToRow;
        private Dictionary<uint, uint> MapToRow {
            get {
                if (_mapToRow != null) {
                    return _mapToRow;
                }

                Item[] unopenedMaps = this.pi.Data.GetExcelSheet<Item>()
                    .Where(item => item.FilterGroup == 18) // this is the filter group for maps
                    .Where(item => item.RowId != 24_794) // exclude the seemingly special maps
                    .ToArray();
                EventItem[] openedMaps = this.pi.Data.GetExcelSheet<EventItem>()
                    .Where(item => item.Unknown13 == 2) // apparently only opened maps have this field
                    .Where(item => item.Unknown5 == 1) // this removes some weird maps and dupes
                    .Where(item => item.RowId != 2_002_503 && item.RowId != 2_002_504) // exclude the seemingly special maps
                    .ToArray();

                Dictionary<uint, uint> mapToRow = new Dictionary<uint, uint>();
                for (int i = 0; i < unopenedMaps.Length; i++) {
                    Item unopened = unopenedMaps[i];
                    EventItem opened = openedMaps[i];

                    // associate the eventitem's id with the additional data of the unopened map, which is the row for the treasure spot table
                    mapToRow[opened.RowId] = unopened.AdditionalData;
                }

                _mapToRow = mapToRow;

                return _mapToRow;
            }
        }

        private readonly DalamudPluginInterface pi;
        private readonly Configuration config;
        private TreasureMapPacket lastMap;

        public TreasureMaps(DalamudPluginInterface pi, Configuration config) {
            this.pi = pi ?? throw new ArgumentNullException(nameof(pi), "DalamudPluginInterface cannot be null");
            this.config = config ?? throw new ArgumentNullException(nameof(config), "Configuration cannot be null");
        }

        public void OnHover(object sender, ulong id) {
            if (!this.config.ShowOnHover || this.lastMap == null || this.lastMap.EventItemId != id) {
                return;
            }

            this.OpenMapLocation();
        }

        public void OnNetwork(IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction) {
            if (direction != NetworkMessageDirection.ZoneDown) {
                return;
            }

            TreasureMapPacket packet = ParsePacket(dataPtr, opCode);
            if (packet == null) {
                return;
            }

            this.lastMap = packet;

            if (this.config.ShowOnOpen && packet.JustOpened) {
                // this does not work because the offset in memory is not yet updated with the thing
                this.OpenMapLocation();
            }
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

            // TODO: can probably fix this up to be nicer after Dalamud releases a new version and fixes the big with MapLinkPayload

            float x = ToMapCoordinate(loc.X, map.SizeFactor);
            float y = ToMapCoordinate(loc.Z, map.SizeFactor);
            MapLinkPayload mapLink = new MapLinkPayload(
                terr.RowId,
                map.RowId,
                ConvertMapCoordinateToRawPosition(x, map.SizeFactor),
                ConvertMapCoordinateToRawPosition(y, map.SizeFactor)
            );

            // fix bug in Dalamud
            mapLink.GetType().GetField("DataResolver", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(mapLink, this.pi.Data);

            this.pi.Framework.Gui.OpenMapWithMapLink(mapLink);
        }

        public static TreasureMapPacket ParsePacket(IntPtr dataPtr, ushort opCode) {
            if (opCode != 0x165) {
                return null;
            }

            uint category = (uint)Marshal.ReadByte(dataPtr);
            if (category != 0x54) {
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

            var scaledPos = ((((pos - 1.0f) * c / 41.0f) * 2048.0f) - 1024.0f) / c;
            scaledPos *= 1000.0f;

            return (int)scaledPos;
        }
        private static float ToMapCoordinate(float val, float scale) {
            var c = scale / 100f;

            val *= c;
            return ((41f / c) * ((val + 1024f) / 2048f)) + 1;
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
