using System;

namespace BIS.Core.Compression
{
    public static class LZSS
    {
        public static uint ReadLZSS(System.IO.Stream input, out byte[] dst, uint expectedSize, bool useSignedChecksum)
        {
            const int N = 4096;
            const int F = 18;
            const int THRESHOLD = 2;
            char[] text_buf = new char[N+F-1];
            dst = new byte[expectedSize];

            if( expectedSize<=0 ) return 0;

            var startPos = input.Position;
            var bytesLeft = expectedSize;
            int iDst = 0;

            int i,j,r,c,csum=0;
            int flags;
            for( i=0; i<N-F; i++ ) text_buf[i] = ' ';
            r=N-F; flags=0;
            while( bytesLeft>0 )
            {
                if( ((flags>>= 1)&256)==0 )
                {
                    c=input.ReadByte();
                    flags=c|0xff00;
                }
                if( (flags&1) != 0)
                {
                    c=input.ReadByte();
                    if (useSignedChecksum)
                        csum += (sbyte)c;
                    else
                        csum += (byte)c;

                    // save byte
                    dst[iDst++]=(byte)c;
                    bytesLeft--;
                    // continue decompression
                    text_buf[r]=(char)c;
                    r++;r&=(N-1);
                }
                else
                {
                    i=input.ReadByte();
                    j=input.ReadByte();
                    i|=(j&0xf0)<<4; j&=0x0f; j+=THRESHOLD;

                    int ii = r-i;
                    int jj = j+ii;

                    if (j+1>bytesLeft)
                    {
                        throw new ArgumentException("LZSS overflow");
                    }

                    for(; ii<=jj; ii++ )
                    {
                        c=(byte)text_buf[ii&(N-1)];
                        if (useSignedChecksum)
                            csum += (sbyte)c;
                        else
                            csum += (byte)c;

                        // save byte
                        dst[iDst++]=(byte)c;
                        bytesLeft--;
                        // continue decompression
                        text_buf[r]=(char)c;
                        r++;r&=(N-1);
                    }
                }
            }

            var csData = new byte[4];
            input.Read(csData,0,4);
            int csr = BitConverter.ToInt32(csData, 0);

            if( csr!=csum )
            {
                throw new ArgumentException("Checksum mismatch");
            }

            return (uint)(input.Position - startPos);
        }
    }
}
