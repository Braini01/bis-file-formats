using BIS.Core;
using BIS.Core.Streams;
using System.Diagnostics;

namespace BIS.PAA
{
    public class Palette
    {
        public const int PicFlagAlpha = 1;
        public const int PicFlagTransparent = 2;

        public PackedColor[] Colors { get; private set; }

        public PackedColor AverageColor { get; private set; }
        //! color used to maximize dynamic range
        public PackedColor MaxColor { get; private set; }

        internal ARGBSwizzle ChannelSwizzle { get; private set; } = ARGBSwizzle.Default;

        public bool IsAlpha { get; private set; }
        public bool IsTransparent { get; private set; }


        public Palette(PAAType format)
        {
            MaxColor = new PackedColor(0xffffffff);
            switch (format)
            {
                case PAAType.RGBA_4444:
                case PAAType.RGBA_8888:
                case PAAType.AI88:
                    AverageColor = new PackedColor(0x80c02020);
                    break;
                default:
                    AverageColor = new PackedColor(0xff802020);
                    break;
            }
        }

        public void Read(BinaryReaderEx input, int[] startOffsets)
        {
            //read Taggs
            while (input.ReadAscii(4) == "GGAT")
            {
                var taggName = input.ReadAscii(4);
                int taggSize = input.ReadInt32();

                switch (taggName)
                {
                    case "CXAM": //MAXC
                        Debug.Assert(taggSize == 4);
                        MaxColor = new PackedColor(input.ReadUInt32());
                        break;
                    case "CGVA": //AVGC
                        Debug.Assert(taggSize == 4);
                        AverageColor = new PackedColor(input.ReadUInt32());
                        break;
                    case "GALF": //FLAG
                        Debug.Assert(taggSize == 4);

                        int flags = input.ReadInt32();
                        if ((flags & PicFlagAlpha) != 0)
                            IsAlpha = true;
                        if ((flags & PicFlagTransparent) != 0)
                            IsTransparent = true;
                        break;

                    case "SFFO": //OFFS
                        int nOffs = taggSize / sizeof(int);
                        for (int i = 0; i < nOffs; i++)
                        {
                            startOffsets[i] = input.ReadInt32();
                        }
                        break;
                    case "ZIWS": //SWIZ
                        Debug.Assert(taggSize == 4);
                        ARGBSwizzle newSwizzle;
                        newSwizzle.SwizA = (TexSwizzle)input.ReadByte();
                        newSwizzle.SwizR = (TexSwizzle)input.ReadByte();
                        newSwizzle.SwizG = (TexSwizzle)input.ReadByte();
                        newSwizzle.SwizB = (TexSwizzle)input.ReadByte();
                        ChannelSwizzle = newSwizzle;
                        break;

                    default:
                        //just skip the data
                        Debug.Fail("What is that unknown PAA tagg?");
                        input.Position += taggSize;
                        break;
                }
            }
            input.Position -= 4;

            //read palette colors
            var nPaletteColors = input.ReadUInt16();
            Colors = new PackedColor[nPaletteColors];
            for (int index = 0; index < nPaletteColors; ++index)
            {
                var b = input.ReadByte();
                var g = input.ReadByte();
                var r = input.ReadByte();
                Colors[index] = new PackedColor(r, g, b);
            }
        }
    }
}
