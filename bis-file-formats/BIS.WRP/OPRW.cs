using System;
using System.IO;
using System.Diagnostics;

using BIS.Core;
using BIS.Core.Math;
using BIS.Core.Streams;
using System.Linq;
using System.Collections.Generic;

namespace BIS.WRP
{
    public class OPRW
    {
        public int Version { get; private set; }
        public int AppID { get; private set; }
        public int LandRangeX { get; private set; }
        public int LandRangeY { get; private set; }
        public int TerrainRangeX { get; private set; }
        public int TerrainRangeY { get; private set; }
        public float LandGrid { get; private set; }
        public QuadTree<GeographyInfo> Geography { get; private set; }
        public QuadTree<byte> SoundMap { get; private set; }
        public Vector3P[] Mountains { get; private set; } //map peaks
        public QuadTree<short> Materials { get; private set; }
        public byte[] Random { get; private set; } //short values
        public byte[] GrassApprox { get; private set; }
        public byte[] PrimTexIndex { get; private set; } //coord to primary texture mapping
        public byte[] Elevation { get; private set; }
        public string[] MatNames { get; private set; }
        public string[] Models { get; private set; }
        public StaticEntityInfo[] EntityInfos { get; private set; }
        public QuadTree<int> ObjectOffsets { get; private set; }
        public QuadTree<int> MapObjectOffsets { get; private set; }
        public byte[] Persistent { get; private set; }
        public int MaxObjectId { get; private set; }
        public RoadLink[][] Roadnet { get; private set; }
        public Object[] Objects { get; private set; }
        public byte[] MapInfos { get; private set; }

        public OPRW(Stream s)
        {
            var input = new BinaryReaderEx(s);
            Read(input);

            // version 3 - OFP Retail landscape (no streaming, no map)
            // version 5 - OFP XBox landscape beta (streaming, no map)
            // version 6 - landscape (streaming and map)
            // version 7 - landscape, including roads (streaming and map)
            // version 10 - landscape, quad trees 
            // version 11 - landscape, changed geography
            // version 12 - OFP Xbox/FP2 landscape, different grid for textures and terrain
            // version 13 - landscape, subdivision hints included
            // version 14 - landscape, skew object flag added
            // version 15 - landscape, entity list added
            // version 16 - ArmA landscape, roads transform + LODShape added
            // version 17 - major texture pass added
            // version 18 - grass map added, float used as raw data
            // version 19 - water depth geography info change
            // version 20 - grass map contains flat areas around roads
            // version 21 - randomization array removed
            // version 22 - primary texture info added
            // version 23 - LZO compression used for compressed arrays
            // version 24 - extended info for roads (connection types)
            // version 25 - appID of the app or DLC the map belongs
            // version 26 - offset table at the beginning (_VBS3_WRP_OFFSET_TABLE), heightmap compression (_VBS3_HEIGHTMAP_COMPRESSION)
            // version 27 - storing of large static objects R-tree in wrp <-- NOTE: technology not used. Implemented without need of WRP changes!
        }

        //minimal version 10
        private void Read(BinaryReaderEx input)
        {
            var fileSig = input.ReadAscii(4);
            if (fileSig != "OPRW") throw new FormatException("OPRW file does not start with correct file signature");
            Version = input.ReadInt32();
            input.Version = Version;
            if (Version < 10) throw new NotSupportedException("OPRW file versions below 10 are not supported");

            if (Version >= 23) input.UseLZOCompression = true;
            //if (version >= 25) input.UseCompressionFlag = true;

            if (Version >= 25)
                AppID = input.ReadInt32();

            if (Version >= 12)
            {
                LandRangeX = input.ReadInt32();
                LandRangeY = input.ReadInt32(); //same as x?
                TerrainRangeX = input.ReadInt32();
                TerrainRangeY = input.ReadInt32(); //same as x?
                LandGrid = input.ReadSingle();
                Debug.Assert(LandRangeX == LandRangeY && TerrainRangeX == TerrainRangeY);
            }

            Geography = new QuadTree<GeographyInfo>(LandRangeX, LandRangeY, input, (src, off) => BitConverter.ToInt16(src, off), 2);
            //if(version<19) transformOldWaterInformation

            var soundMapCoef = 1; //ToDo: this is read from config
            SoundMap = new QuadTree<byte>(LandRangeX * soundMapCoef, LandRangeX * soundMapCoef, input, (src,off) => src[off],1); //both landRangeX are correct. no mistake

            Mountains = input.ReadArray(inp => new Vector3P(inp));

            Materials = new QuadTree<short>(LandRangeX, LandRangeY, input, (src, off) => BitConverter.ToInt16(src, off), 2);

            if (Version < 21)
                Random = input.ReadCompressed((uint)(LandRangeX * LandRangeY * 2)); //short values

            if (Version >= 18)
                GrassApprox = input.ReadCompressed((uint)(TerrainRangeX * TerrainRangeY)); //byte values

            if (Version >= 22)
                PrimTexIndex = input.ReadCompressed((uint)(TerrainRangeX * TerrainRangeY)); //signed byte values?

            Elevation = input.ReadCompressed((uint)(TerrainRangeX * TerrainRangeY * 4));

            var nMaterials = input.ReadInt32();
            MatNames = new string[nMaterials];
            var major = new byte[nMaterials];
            for (int i = 0; i < nMaterials; i++)
            {
                MatNames[i] = input.ReadAsciiz();
                major[i] = input.ReadByte();
            }

            Models = input.ReadStringArray();

            if (Version >= 15)
            {
                EntityInfos = input.ReadArray(inp => new StaticEntityInfo(inp));
            }

            ObjectOffsets = new QuadTree<int>(LandRangeX, LandRangeY, input, (src, off) => BitConverter.ToInt32(src, off), 4);
            var sizeOfObjects = input.ReadInt32();
            MapObjectOffsets = new QuadTree<int>(LandRangeX, LandRangeY, input, (src, off) => BitConverter.ToInt32(src, off), 4);
            var sizeOfMapinfo = input.ReadInt32();

            Persistent = input.ReadCompressed((uint)(LandRangeX * LandRangeY));
            var subDivHints = input.ReadCompressed((uint)(TerrainRangeX * TerrainRangeY));

            MaxObjectId = input.ReadInt32();
            var roadnetSize = input.ReadInt32();

            Roadnet = new RoadLink[LandRangeX * LandRangeY][];
            var pos = input.Position;
            for (int i = 0; i < LandRangeX * LandRangeY; i++)
            {
                Roadnet[i] = input.ReadArray( inp => new RoadLink(inp) );
            }
            var read = input.Position - pos;

            var nObjects = sizeOfObjects / 60;
            Objects = new Object[nObjects];

            for (int i = 0; i < nObjects; i++)
            {
                Objects[i] = new Object(input);
            }

            MapInfos = input.ReadBytes((int)(input.BaseStream.Length - input.BaseStream.Position));
        }
    }
}