using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace BIS.PAA
{
    internal enum TexSwizzle: byte
    {
        TSAlpha,
        TSRed,
        TSGreen,
        TSBlue,
        TSInvAlpha,
        TSInvRed,
        TSInvGreen,
        TSInvBlue,
        TSOne
    }

    internal struct ARGBSwizzle
    {
        internal TexSwizzle SwizB;
        internal TexSwizzle SwizG;
        internal TexSwizzle SwizR;
        internal TexSwizzle SwizA;

        internal TexSwizzle this[int ch]
        {
            get
            {
                switch(ch)
                {
                    case 0: return SwizA;
                    case 1: return SwizR;
                    case 2: return SwizG;
                    case 3: return SwizB;
                    default: throw new ArgumentOutOfRangeException();
                }
            }

            set
            {
                switch (ch)
                {
                    case 0: SwizA = value; break;
                    case 1: SwizR = value; break;
                    case 2: SwizG = value; break;
                    case 3: SwizB = value; break;
                    default: throw new ArgumentOutOfRangeException();
                }
            }
        }


        static ARGBSwizzle()
        {
            Default.SwizA = TexSwizzle.TSAlpha;
            Default.SwizR = TexSwizzle.TSRed;
            Default.SwizG = TexSwizzle.TSGreen;
            Default.SwizB = TexSwizzle.TSBlue;
        }

        internal static ARGBSwizzle Default;
    }

    internal static class ChannelSwizzling
    {
        internal static void Apply(byte[] argbPixels, ARGBSwizzle swizzle)
        {
            ARGBSwizzle invSwizzle = ARGBSwizzle.Default;
            InvertSwizzle(ref invSwizzle, in swizzle, 0);
            InvertSwizzle(ref invSwizzle, in swizzle, 1);
            InvertSwizzle(ref invSwizzle, in swizzle, 2);
            InvertSwizzle(ref invSwizzle, in swizzle, 3);
            ChannelSwizzle(in invSwizzle, argbPixels);
        }

        private static void InvertSwizzle(ref ARGBSwizzle invSwizzle, in ARGBSwizzle swizzle, byte ch)
        {
            TexSwizzle swiz = TexSwizzle.TSAlpha + ch;
            if (swizzle[ch] >= TexSwizzle.TSInvAlpha && swizzle[ch] <= TexSwizzle.TSInvBlue)
            {
                invSwizzle[swizzle[ch] - TexSwizzle.TSInvAlpha] = TexSwizzle.TSInvAlpha - TexSwizzle.TSAlpha + swiz;
            }
            else if (swizzle[ch] <= TexSwizzle.TSBlue)
            {
                invSwizzle[(int)swizzle[ch]] = swiz;
            }
        }

        private static (int offset, int mul, int add) CheckInvSwizzle(TexSwizzle swiz)
        {
            if (swiz == TexSwizzle.TSOne)
            {
                // one - ignore input (mul by 0) and set it to one (add 255)
                return (0, 0, 255);
            }
            int mul = 1;
            int add = 0;
            switch (swiz)
            {
                case TexSwizzle.TSInvAlpha: swiz = TexSwizzle.TSAlpha; mul = -1; add = 255; break;
                case TexSwizzle.TSInvRed: swiz = TexSwizzle.TSRed; mul = -1; add = 255; break;
                case TexSwizzle.TSInvGreen: swiz = TexSwizzle.TSGreen; mul = -1; add = 255; break;
                case TexSwizzle.TSInvBlue: swiz = TexSwizzle.TSBlue; mul = -1; add = 255; break;
            }
            int offset = swiz < TexSwizzle.TSOne ? 24 - (int)swiz * 8 : 0;

            return (offset, mul, add);
        }

        private static void ChannelSwizzle(in ARGBSwizzle channelSwizzle, byte[] argbPixels)
        {
            if (channelSwizzle[0] == TexSwizzle.TSAlpha && channelSwizzle[1] == TexSwizzle.TSRed &&
              channelSwizzle[2] == TexSwizzle.TSGreen && channelSwizzle[3] == TexSwizzle.TSBlue)
            {
                return;
            }

            (int aOffset, int mulA, int addA) = CheckInvSwizzle(channelSwizzle[0]);
            (int rOffset, int mulR, int addR) = CheckInvSwizzle(channelSwizzle[1]);
            (int gOffset, int mulG, int addG) = CheckInvSwizzle(channelSwizzle[2]);
            (int bOffset, int mulB, int addB) = CheckInvSwizzle(channelSwizzle[3]);

            int nPixel = argbPixels.Length / 4;
            while (--nPixel >= 0)
            {
                int pixOffset = nPixel * 4;
                int p = BitConverter.ToInt32(argbPixels, pixOffset);
                int a = (p >> aOffset) & 0xff;
                int r = (p >> rOffset) & 0xff;
                int g = (p >> gOffset) & 0xff;
                int b = (p >> bOffset) & 0xff;

                argbPixels[pixOffset] = (byte)(b * mulB + addB);
                argbPixels[pixOffset + 1] = (byte)(g * mulG + addG);
                argbPixels[pixOffset + 2] = (byte)(r * mulR + addR);
                argbPixels[pixOffset + 3] = (byte)(a * mulA + addA);
            }
        }
    }
}
