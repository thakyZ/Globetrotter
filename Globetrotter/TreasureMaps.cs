using Dalamud.Game.Chat.SeStringHandling.Payloads;
using Dalamud.Game.Internal.Network;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Globetrotter {
    class TreasureMaps {
        private static readonly Dictionary<uint, uint> MAP_TO_ROW = new Dictionary<uint, uint> {
            [2_001_087] = 1,
            [2_001_088] = 2,
            [2_001_089] = 3,
            [2_001_090] = 4,
            [2_001_091] = 5,
            // missing 6, 7, 8
            [2_001_762] = 9,
            [2_001_763] = 10,
            [2_001_764] = 11,
            [2_002_209] = 12,
            [2_002_210] = 13,
            // missing 14, 15, 16
            [2_002_663] = 17,
            [2_002_664] = 18,
        };

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

            if (!MAP_TO_ROW.TryGetValue(packet.EventItemId, out uint rowId)) {
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
