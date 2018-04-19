using System;
using System.Diagnostics;

namespace BIS.Core.Compression
{
    public static class LZO
    {
        private static readonly uint M2_MAX_OFFSET = 0x0800;

        public static byte[] Decompress(ReadOnlySpan<byte> input, int expectedSize, out int ip)
        {
            var output = new byte[expectedSize];

            int op = 0;
            ip = 0;
            int t;
            int m_pos;

            if (input[ip] > 17)
            {
                #region B_1
                t = input[ip++] - 17;
                if (t < 4) goto match_next;
                #endregion
                #region B_2
                Debug.Assert(t > 0);// returns LZO_E_ERROR;
                if ((expectedSize - op) < (t)) throw new OverflowException("Output Overrun");
                do output[op++] = input[ip++]; while (--t > 0);
                goto first_literal_run;
                #endregion
            }

            B_3:
                #region B_3
                t = input[ip++];
                if (t >= 16) goto match;
                #endregion
                #region B_4
                if (t == 0)
                {
                    while (input[ip] == 0)
                    {
                        t += 255;
                        ip++;
                    }
                    t += 15 + input[ip++];
                }
                Debug.Assert(t > 0);
                if ((expectedSize - op) < (t + 3)) throw new OverflowException("Output Overrun");

                input.Slice(ip, 4).CopyTo(output.AsSpan(op, 4));
                op += 4; ip += 4;
                if (--t > 0)
                {
                if (t >= 4)
                {
                    do
                    {
                        input.Slice(ip, 4).CopyTo(output.AsSpan(op, 4));
                        op += 4; ip += 4; t -= 4;
                    } while (t >= 4);
                    if (t > 0) do output[op++] = input[ip++]; while (--t > 0);
                    }
                    else
                        do output[op++] = input[ip++]; while (--t > 0);
                }
                #endregion

                #region f_l_r
            first_literal_run:
                t = input[ip++];
                if (t >= 16) goto match;
                #endregion

                #region B_5
                m_pos = (int)(op - (1 + M2_MAX_OFFSET));
                m_pos -= t >> 2;
                m_pos -= input[ip++] << 2;

                if (m_pos < 0 || m_pos >= op) throw new OverflowException("Lookbehind Overrun");
                if ((expectedSize - op) < (3)) throw new OverflowException("Output Overrun");
                output[op++] = output[m_pos++]; output[op++] = output[m_pos++]; output[op++] = output[m_pos];

                goto match_done;
                #endregion

        match:
            if (t >= 64)
            {
                #region m_1
                m_pos = op - 1;
                m_pos -= (t >> 2) & 7;
                m_pos -= input[ip++] << 3;
                t = (t >> 5) - 1;
                if (m_pos < 0 || m_pos >= op) throw new OverflowException("Lookbehind Overrun");
                Debug.Assert(t > 0);
                if ((expectedSize - op) < (t + 2)) throw new OverflowException("Output Overrun");
                goto copy_match;
                #endregion
            }
            else if (t >= 32)
            {
                #region m_2
                t &= 31;
                if (t == 0)
                {
                    while (input[ip] == 0)
                    {
                        t += 255;
                        ip++;
                    }
                    t += 31 + input[ip++];
                }

                m_pos = op - 1;
                m_pos -= (input[ip] >> 2) + (input[ip+1] << 6);

                ip += 2;
                #endregion
            }
            else if (t >= 16)
            {
                #region m_3_1
                m_pos = op;
                m_pos -= (t & 8) << 11;

                t &= 7;
                if (t == 0)
                {
                    while (input[ip] == 0)
                    {
                        t += 255;
                        ip++;
                    }
                    t += 7 + input[ip++];
                }

                m_pos -= (input[ip] >> 2) + (input[ip+1] << 6);
                ip += 2;

                if (m_pos == op)
                {
                    #region done1
                    //int val = (int)(op - output);
                    Debug.Assert(t == 1);
                    if (m_pos != expectedSize)
                        throw new OverflowException("Output Underrun");

                    return output;
                    #endregion
                }
                #endregion
                #region m_3_2
                m_pos -= 0x4000;
                #endregion
            }
            else
            {
                #region m_4
                m_pos = op - 1;
                m_pos -= t >> 2;
                m_pos -= input[ip++] << 2;

                if (m_pos < 0 || m_pos >= op) throw new OverflowException("Lookbehind Overrun");
                if ((expectedSize - op) < (2)) throw new OverflowException("Output Overrun");
                output[op++] = output[m_pos++]; output[op++] = output[m_pos];
                goto match_done;
                #endregion
            }

            #region B_6
            if (m_pos < 0 || m_pos >= op) throw new OverflowException("Lookbehind Overrun");
            Debug.Assert(t > 0);
            if ((expectedSize - op) < (t + 2)) throw new OverflowException("Output Overrun");
            #endregion

            if (t >= 2 * 4 - (3 - 1) && (op - m_pos) >= 4)
            {
                #region B_7
                output.AsSpan(m_pos, 4).CopyTo(output.AsSpan(op, 4));
                op += 4; m_pos += 4; t -= 4 - (3 - 1);
                do
                {
                    output.AsSpan(m_pos, 4).CopyTo(output.AsSpan(op, 4));
                    op += 4; m_pos += 4; t -= 4;
                } while (t >= 4);
                if (t > 0) do output[op++] = output[m_pos++]; while (--t > 0);
                goto match_done;
                #endregion
            }

        copy_match:
            output[op++] = output[m_pos++]; output[op++] = output[m_pos++];
            do output[op++] = output[m_pos++]; while (--t > 0);

        match_done:
            t = input[ip-2] & 3;
            if (t == 0) goto B_3;

        match_next:
            Debug.Assert(t > 0 && t < 4);
            if ((expectedSize - op) < (t)) throw new OverflowException("Output Overrun");

            output[op++] = input[ip++];
            if (t > 1) { output[op++] = input[ip++]; if (t > 2) { output[op++] = input[ip++]; } }

            t = input[ip++];
            goto match;
        }


        private static byte ip(System.IO.Stream i)
        {
            byte b = (byte)i.ReadByte();
            i.Position--;
            return b;
        }
        private static byte ip(System.IO.Stream i, short offset)
        {
            i.Position += offset;
            byte b = (byte)i.ReadByte();
            i.Position -= offset + 1;
            return b;
        }
        private static byte next(System.IO.Stream i)
        {
            return (byte)i.ReadByte();
        }

        public unsafe static uint Decompress(System.IO.Stream i, byte* output, uint expectedSize)
        {
            long startPos = i.Position;
            byte* op;
            uint t;
            byte* m_pos;

            byte* op_end = output + expectedSize;
            op = output;

            if (ip(i) > 17)
            {
                t = next(i) - 17U;
                if (t < 4) goto match_next;

                Debug.Assert(t > 0);
                if ((op_end - op) < (t)) throw new OverflowException("Outpur Overrun");
                do *op++ = next(i); while (--t > 0);
                goto first_literal_run;
            }

        B_3:
            t = next(i);
            if (t >= 16) goto match;

            if (t == 0)
            {
                while (ip(i) == 0)
                {
                    t += 255;
                    i.Position++;
                }
                t += 15U + next(i);
            }
            Debug.Assert(t > 0);
            if ((op_end - op) < (t + 3)) throw new OverflowException("Output Overrun");

            *op++ = next(i);
            *op++ = next(i);
            *op++ = next(i);
            *op++ = next(i);
            if (--t > 0)
            {
                if (t >= 4)
                {
                    do
                    {
                        *op++ = next(i);
                        *op++ = next(i);
                        *op++ = next(i);
                        *op++ = next(i); 
                        t -= 4;
                    } while (t >= 4);
                    if (t > 0) do *op++ = next(i); while (--t > 0);
                }
                else
                    do *op++ = next(i); while (--t > 0);
            }

        first_literal_run:
            t = next(i);
            if (t >= 16) goto match;

            m_pos = op - (1 + M2_MAX_OFFSET);
            m_pos -= t >> 2;
            m_pos -= next(i) << 2;

            if (m_pos < output || m_pos >= op) throw new OverflowException("Lookbehind Overrun");
            if ((op_end - op) < (3)) throw new OverflowException("Output Overrun");
            *op++ = *m_pos++; *op++ = *m_pos++; *op++ = *m_pos;

            goto match_done;

        match:
            if (t >= 64)
            {
                m_pos = op - 1;
                m_pos -= (t >> 2) & 7;
                m_pos -= next(i) << 3;
                t = (t >> 5) - 1;
                if (m_pos < output || m_pos >= op) throw new OverflowException("Lookbehind Overrun");
                Debug.Assert(t > 0);
                if ((op_end - op) < (t + 2)) throw new OverflowException("Output Overrun");
                goto copy_match;
            }
            else if (t >= 32)
            {
                t &= 31;
                if (t == 0)
                {
                    while (ip(i) == 0)
                    {
                        t += 255;
                        i.Position++;
                    }
                    t += 31U + next(i);
                }

                m_pos = op - 1;
                m_pos -= (ip(i,0) >> 2) + (ip(i,1) << 6);

                i.Position += 2;
            }
            else if (t >= 16)
            {
                m_pos = op;
                m_pos -= (t & 8) << 11;

                t &= 7;
                if (t == 0)
                {
                    while (ip(i) == 0)
                    {
                        t += 255;
                        i.Position++;
                    }
                    t += 7U + next(i);
                }

                m_pos -= (ip(i,0) >> 2) + (ip(i,1) << 6);

                i.Position += 2;

                //compression done?
                if (m_pos == op)
                {
                    int val = (int)(op - output);
                    Debug.Assert(t == 1);
                    if (m_pos != op_end)
                        throw new OverflowException("Output Underrun");
                    return (uint)(i.Position-startPos);
                }
                m_pos -= 0x4000;
            }
            else
            {
                m_pos = op - 1;
                m_pos -= t >> 2;
                m_pos -= next(i) << 2;

                if (m_pos < output || m_pos >= op) throw new OverflowException("Lookbehind Overrun");
                if ((op_end - op) < (2)) throw new OverflowException("Output Overrun");
                *op++ = *m_pos++; *op++ = *m_pos;
                goto match_done;
            }

            if (m_pos < output || m_pos >= op) throw new OverflowException("Lookbehind Overrun");
            Debug.Assert(t > 0);
            if ((op_end - op) < (t + 2)) throw new OverflowException("Output Overrun");

            if (t >= 2 * 4 - (3 - 1) && (op - m_pos) >= 4)
            {
                *(uint*)(op) = *(uint*)(m_pos);
                op += 4; m_pos += 4; t -= 4 - (3 - 1);
                do
                {
                    *(uint*)(op) = *(uint*)(m_pos);
                    op += 4; m_pos += 4; t -= 4;
                } while (t >= 4);
                if (t > 0) do *op++ = *m_pos++; while (--t > 0);
                goto match_done;
            }

        copy_match:
            *op++ = *m_pos++; *op++ = *m_pos++;
            do *op++ = *m_pos++; while (--t > 0);

        match_done:
            t = ip(i, -2) & 3U;
            if (t == 0) goto B_3;

        match_next:
            Debug.Assert(t > 0 && t < 4);
            if ((op_end - op) < (t)) throw new OverflowException("Output Overrun");

            *op++ = next(i);
            if (t > 1) { *op++ = next(i); if (t > 2) { *op++ = next(i); } }

            t = next(i);
            goto match;
        }


        public unsafe static uint ReadLZO(System.IO.Stream input, out byte[] dst, uint expectedSize)
        {
            dst = new byte[expectedSize];
            fixed (byte* output = &dst[0])
            {
                return Decompress(input,output,expectedSize);
            }
        }

        public unsafe static byte[] ReadLZO(System.IO.Stream input, uint expectedSize)
        {
            var dst = new byte[expectedSize];
            fixed (byte* output = &dst[0])
            {
                Decompress(input, output, expectedSize);
            }

            return dst;
        }
    }
}
