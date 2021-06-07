﻿// From https://github.com/differentrain/LzssStream
/*MIT License

    Copyright(c) 2019 differentrain

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.

 */
using System.Collections.Concurrent;

namespace System.IO.Compression
{
    /// <summary>
    /// Provides methods and properties for compressing and decompressing streams by using the LZSS algorithm.
    /// </summary>
    public class LzssStream : Stream
    {
        private const int DefaultWindowSize = 0x1000;
        private const byte DefaultCharFiller = 0x20; //White space
        private const int DefaultMaxMatchLength = 18;
        private const byte DefaultMatchThresold = 2;


        private readonly CompressionMode _mode;
        private readonly bool _leaveOpen;

        private SaluteToHaruhikoTheOkami _LzssHelper;

        /// <summary>
        /// Gets a reference to the underlying stream.
        /// </summary>
        /// <value>A stream object that represents the underlying stream.</value>
        public Stream BaseStream { get; private set; }

        /// <summary>
        /// The compressed size.
        /// </summary>
        public int LastCodeLength { get; private set; }

        /// <summary>
        /// Gets the size of sliding window. 
        /// <para>Default value is 4096.</para>
        /// </summary>
        protected virtual int WindowSize => DefaultWindowSize;
        /// <summary>
        /// Gets a byte value represents an ASCII char to fill the array.
        /// <para>Default value is 0x20 (White space).</para>
        /// </summary>
        protected virtual byte CharFiller => DefaultCharFiller;
        /// <summary>
        /// Gets the upper limit for the length of match.
        /// <para>Default value is 18.</para>
        /// </summary>
        protected virtual int MaxMatchLength => DefaultMaxMatchLength;
        /// <summary>
        /// Gets the value of triggering threshold if the match length is greater than whom should be encoded.
        /// <para>Default value is 2.</para>
        /// </summary>
        protected virtual byte MatchThresold => DefaultMatchThresold;


        /// <summary>
        /// Initializes a new instance of the <see cref="LzssStream"/> class by using the specified stream and compression mode.
        /// </summary>
        /// <param name="stream">The stream to compress or decompress.</param>
        /// <param name="mode">One of the enumeration values that indicates whether to compress or decompress the stream.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="mode"/> is a valid <see cref="CompressionMode"/> value.
        /// <para>-or-</para>
        /// <see cref="CompressionMode"/> is <see cref="CompressionMode.Compress"/> and <see cref="CanWrite"/> is <c>false</c>.
        /// <para>-or-</para>
        /// <see cref="CompressionMode"/> is <see cref="CompressionMode.Decompress"/> and <see cref="CanRead"/> is <c>false</c>.
        /// </exception>
        public LzssStream(Stream stream, CompressionMode mode) : this(stream, mode, false) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LzssStream"/> class by using the specified stream and compression mode, and optionally leaves the stream open.
        /// </summary>
        /// <param name="stream">The stream to compress or decompress.</param>
        /// <param name="mode">One of the enumeration values that indicates whether to compress or decompress the stream.</param>
        /// <param name="leaveOpen"><c>true</c> to leave the stream open after disposing the DeflateStream object; otherwise, <c>false</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="mode"/> is a valid <see cref="CompressionMode"/> value.
        /// <para>-or-</para>
        /// <see cref="CompressionMode"/> is <see cref="CompressionMode.Compress"/> and <see cref="CanWrite"/> is <c>false</c>.
        /// <para>-or-</para>
        /// <see cref="CompressionMode"/> is <see cref="CompressionMode.Decompress"/> and <see cref="CanRead"/> is <c>false</c>.
        /// </exception>
        public LzssStream(Stream stream, CompressionMode mode, bool leaveOpen)
        {
            if (CompressionMode.Compress != mode && CompressionMode.Decompress != mode) throw new ArgumentException("mode is a valid CompressionMode value.", "mode");

            BaseStream = stream ?? throw new ArgumentNullException("stream");
            _mode = mode;
            _leaveOpen = leaveOpen;

            switch (_mode)
            {
                case CompressionMode.Decompress:
                    if (!BaseStream.CanRead) throw new ArgumentException("mode is CompressionMode.Decompress and CanRead is false.", "steam");
                    break;
                case CompressionMode.Compress:

                    if (!BaseStream.CanWrite) throw new ArgumentException("mode is CompressionMode.Compress and CanWrite is false.", "steam");
                    break;
            }
            _LzssHelper = SaluteToHaruhikoTheOkami.RentInstance(this);
        }

        /// <summary>
        /// Reads a number of decompressed bytes into the specified byte array.
        /// </summary>
        /// <param name="array">The array to store decompressed bytes.</param>
        /// <param name="offset">The byte offset in <paramref name="array"/> at which the read bytes will be placed.</param>
        /// <param name="count">The maximum number of decompressed bytes to read.</param>
        /// <returns>The number of bytes that were read into the byte array.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="CompressionMode"/> value was <see cref="CompressionMode.Compress"/> when the object was created.
        /// <para>-or-</para>
        /// The underlying stream does not support reading.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="offset"/> or <paramref name="count"/> is less than zero.
        /// <para>-or-</para>
        /// <paramref name="array"/> length minus the index starting point is less than <paramref name="count"/>.
        /// </exception>
        /// <exception cref="InvalidDataException">The data is in an invalid format.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public override int Read(byte[] array, int offset, int count)
        {
            CheckReadAndWriteMethodCore(array, offset, count, CompressionMode.Compress);
            return _LzssHelper.Decode(array, offset, count, BaseStream);
        }


        /// <summary>
        /// Writes compressed bytes to the underlying stream from the specified byte array.
        /// </summary>
        /// <param name="array">The buffer that contains the data to compress.</param>
        /// <param name="offset">The byte offset in <paramref name="array"/> from which the bytes will be read.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="CompressionMode"/> value was <see cref="CompressionMode.Decompress"/> when the object was created.
        /// <para>-or-</para>
        /// The underlying stream does not support writing.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="offset"/> or <paramref name="count"/> is less than zero.
        /// <para>-or-</para>
        /// <paramref name="array"/> length minus the index starting point is less than <paramref name="count"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public override void Write(byte[] array, int offset, int count)
        {
            CheckReadAndWriteMethodCore(array, offset, count, CompressionMode.Decompress);
            LastCodeLength = _LzssHelper.Encode(array, offset, count, BaseStream);
        }



        private void EnsureNotDisposed()
        {
            if (BaseStream == null) throw new ObjectDisposedException(null, "the Stream is closed.");
        }

        private void CheckReadAndWriteMethodCore(byte[] array, int offset, int count, CompressionMode wrongMode)
        {
            EnsureNotDisposed();
            if (array == null)
                throw new ArgumentNullException("array");

            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset");

            if (count < 0)
                throw new ArgumentOutOfRangeException("count");

            if (array.Length - offset < count)
                throw new ArgumentException("array length minus the index starting point is less than count.");

            if (_mode == wrongMode) throw new InvalidOperationException($"The CompressionMode value was {wrongMode} when the object was created.");
        }

        /// <summary>
        /// Dispose mode.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (_LzssHelper != null)
                    {
                        SaluteToHaruhikoTheOkami.ReturnInstance(_LzssHelper);
                    }
                    if (!_leaveOpen && BaseStream != null)
                    {
                        BaseStream.Dispose();
                    }
                }
            }
            finally
            {
                _LzssHelper = null;
                BaseStream = null;
                base.Dispose(disposing);
            }
        }

        #region implements the members of base class.

        /// <summary>
        /// Gets a value indicating whether the stream supports reading while decompressing a file.
        /// </summary>
        /// <value><c>true</c> if the <see cref="CompressionMode"/> value is <see cref="CompressionMode.Decompress"/>, and the underlying stream is opened and supports reading; otherwise, <c>false</c>.</value>
        public override bool CanRead => BaseStream != null && _mode == CompressionMode.Decompress && BaseStream.CanRead;

        /// <summary>
        /// Gets a value indicating whether the stream supports seeking.
        /// </summary>
        /// <value><c>false</c> in all cases.</value>
        public override bool CanSeek => false;

        /// <summary>
        /// Gets a value indicating whether the stream supports writing.
        /// </summary>
        /// <value><c>true</c> if the <see cref="CompressionMode"/> value is <see cref="CompressionMode.Compress"/>, and the underlying stream is opened and supports writing; otherwise, <c>false</c>. </value>
        public override bool CanWrite => BaseStream != null && _mode == CompressionMode.Compress && BaseStream.CanWrite;

        /// <summary>
        /// This property is not supported and always throws a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <value>A long value.</value>
        /// <exception cref="NotSupportedException">This property is not supported on this stream.</exception>
        public override long Length => throw new NotSupportedException();

        /// <summary>
        /// This property is not supported and always throws a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <value>A long value.</value>
        /// <exception cref="NotSupportedException">This property is not supported on this stream.</exception>
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        /// <summary>
        /// The current implementation of this method has no functionality.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The Stream is closed.</exception>
        public override void Flush() => EnsureNotDisposed();

        /// <summary>
        /// This operation is not supported and always throws a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <param name="offset">The location in the stream.</param>
        /// <param name="origin">One of the <see cref="SeekOrigin"/> values.</param>
        /// <returns>A long value.</returns>
        /// <exception cref="NotSupportedException">This operation is not supported on this stream.</exception>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <summary>
        /// This operation is not supported and always throws a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <param name="value">The length of the stream.</param>
        /// <exception cref="NotSupportedException">This operation is not supported on this stream.</exception>
        public override void SetLength(long value) => throw new NotSupportedException();

        #endregion



        /// <summary>
        /// The people who have ever known about lzss certainly know the name Haruhiko Okumura, and his "lzss.c". 
        /// As a foolish mortal, I just copy his code here directly.
        /// </summary>
        private sealed class SaluteToHaruhikoTheOkami
        {

            #region object pool

            private static readonly ConcurrentDictionary<LzssOption, ConcurrentBag<SaluteToHaruhikoTheOkami>>
                                                                   _instancePool = new ConcurrentDictionary<LzssOption, ConcurrentBag<SaluteToHaruhikoTheOkami>>();


            public static SaluteToHaruhikoTheOkami RentInstance(LzssStream lzssStream)
            {
                var lOpion = new LzssOption(lzssStream);

                var bag = _instancePool.GetOrAdd(lOpion, new ConcurrentBag<SaluteToHaruhikoTheOkami>());

                if (!bag.TryTake(out var inst))
                {
                    inst = new SaluteToHaruhikoTheOkami(lOpion);
                }
                return inst;
            }

            public static void ReturnInstance(SaluteToHaruhikoTheOkami inst) => _instancePool[inst._lzssOption].Add(inst);

            private sealed class LzssOption
            {
                /// <summary>
                /// size of ring buffer
                /// </summary>
                public int N { get; }
                /// <summary>
                /// upper limit for match_length
                /// </summary>
                public int F { get; }
                /// <summary>
                /// encode string into position and length if match_length is greater than this
                /// </summary>
                public int THRESHOLD { get; }
                /// <summary>
                ///  Clear the buffer with any character that will appear often.
                /// </summary>
                public byte FILL { get; }

                public LzssOption(LzssStream lzss)
                {
                    N = lzss.WindowSize;
                    F = lzss.MaxMatchLength;
                    THRESHOLD = lzss.MatchThresold;
                    FILL = lzss.CharFiller;
                }

                public override bool Equals(object obj) => (!(obj is LzssOption inst) ||
                                            inst.FILL != this.FILL ||
                                            inst.N != this.N ||
                                            inst.F != this.F ||
                                            inst.THRESHOLD != this.THRESHOLD
                                            ) ? false : true;

                public override int GetHashCode() => CombineHashCodes(FILL, N, F, THRESHOLD);

                private static int CombineHashCodes(params int[] hashCodes)
                {
                    int hash1 = (5381 << 16) + 5381;
                    int hash2 = hash1;

                    int i = 0;
                    foreach (var hashCode in hashCodes)
                    {
                        if (i % 2 == 0)
                            hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ hashCode;
                        else
                            hash2 = ((hash2 << 5) + hash2 + (hash2 >> 27)) ^ hashCode;

                        ++i;
                    }
                    return hash1 + (hash2 * 1566083941);
                }
            }


            #endregion

            private readonly LzssOption _lzssOption;

            /// <summary>
            /// index for root of binary search trees
            /// </summary>
            private readonly int NIL;

            /// <summary>
            /// ring buffer of size N, with extra F-1 bytes to facilitate string comparison.
            /// </summary>
            private readonly byte[] text_buf;

            /// <summary>
            /// left children &amp; right children &amp; parents -- These constitute binary search trees.
            /// </summary>
            private readonly int[] lson, rson, dad;

            /// <summary>
            /// of longest match.  These are set by the InsertNode() procedure.
            /// </summary>
            private int match_position, match_length;

            private SaluteToHaruhikoTheOkami(LzssOption lzssOp)
            {
                _lzssOption = lzssOp;
                NIL = _lzssOption.N;
                text_buf = new byte[_lzssOption.N + _lzssOption.F - 1];
                lson = new int[_lzssOption.N + 1];
                rson = new int[_lzssOption.N + 257];
                dad = new int[_lzssOption.N + 1];
            }

            /// <summary>
            /// initialize trees.
            /// </summary>
            private void InitTree()
            {
                int i;

                /* For i = 0 to N - 1, rson[i] and lson[i] will be the right and
                   left children of node i.  These nodes need not be initialized.
                   Also, dad[i] is the parent of node i.  These are initialized to
                   NIL (= N), which stands for 'not used.'
                   For i = 0 to 255, rson[N + i + 1] is the root of the tree
                   for strings that begin with character i.  These are initialized
                   to NIL.  Note there are 256 trees. */

                unsafe
                {
                    fixed (int* pr = rson, pd = dad)
                    {
                        for (i = _lzssOption.N + 1; i <= _lzssOption.N + 256; i++)
                        {
                            pr[i] = NIL;
                        }

                        for (i = 0; i < _lzssOption.N; i++)
                        {
                            pd[i] = NIL;
                        }
                    }
                }
            }

            private void InsertNode(int r)
            {
                /* Inserts string of length F, text_buf[r..r+F-1], into one of the
               trees (text_buf[r]'th tree) and returns the longest-match position
               and length via the global variables match_position and match_length.
               If match_length = F, then removes the old node in favor of the new
               one, because the old one will be deleted sooner.
               Note r plays double role, as tree node and position in buffer. */

                int i;
                var cmp = 1;
                match_length = 0;
                unsafe
                {
                    fixed (byte* key = &text_buf[r], pBuf = text_buf)
                    {
                        var p = _lzssOption.N + 1 + key[0];
                        fixed (int* pR = rson, pL = lson, pD = dad)
                        {
                            pR[r] = pL[r] = NIL;
                            for (; ; )
                            {
                                if (cmp >= 0)
                                {
                                    if (pR[p] != NIL)
                                    {
                                        p = pR[p];
                                    }
                                    else
                                    {
                                        pR[p] = r;
                                        pD[r] = p;
                                        return;
                                    }
                                }
                                else
                                {
                                    if (pL[p] != NIL)
                                    {
                                        p = pL[p];
                                    }
                                    else
                                    {
                                        pL[p] = r;
                                        pD[r] = p;
                                        return;
                                    }
                                }
                                for (i = 1; i < _lzssOption.F; i++)
                                {
                                    if ((cmp = key[i] - pBuf[p + i]) != 0) break;
                                }

                                if (i > match_length)
                                {
                                    match_position = p;
                                    if ((match_length = i) >= _lzssOption.F) break;
                                }
                            }
                            pD[r] = pD[p]; pL[r] = pL[p]; pR[r] = pR[p];
                            pD[pL[p]] = r; pD[pR[p]] = r;
                            if (pR[pD[p]] == p)
                            {
                                pR[pD[p]] = r;
                            }
                            else
                            {
                                pL[pD[p]] = r;
                            }
                            pD[p] = NIL;  /* remove p */
                        }
                    }
                }




            }

            /// <summary>
            /// deletes node p from tree
            /// </summary>
            /// <param name="p"></param>
            private void DeleteNode(int p)
            {
                int q;

                unsafe
                {
                    fixed (int* pR = rson, pL = lson, pD = dad)
                    {
                        if (pD[p] == NIL) return;  /* not in tree */
                        if (pR[p] == NIL)
                        {
                            q = pL[p];
                        }
                        else if (pL[p] == NIL)
                        {
                            q = pR[p];
                        }
                        else
                        {
                            q = pL[p];
                            if (pR[q] != NIL)
                            {
                                do
                                {
                                    q = pR[q];
                                } while (pR[q] != NIL);
                                pR[pD[q]] = pL[q]; pD[pL[q]] = pD[q];
                                pL[q] = pL[p]; pD[pL[p]] = q;
                            }
                            pR[q] = pR[p]; pD[pR[p]] = q;
                        }
                        pD[q] = pD[p];
                        if (pR[pD[p]] == p)
                        {
                            pR[pD[p]] = q;
                        }
                        else
                        {
                            pL[pD[p]] = q;
                        }
                        pD[p] = NIL;
                    }
                }


            }


            public int Encode(byte[] array, int offset, int count, Stream stream)
            {
                int i, len, last_match_length;
                byte mask, c;
                var codesize = 0;

                var code_buf_ptr = mask = 1;
                var s = 0;
                var r = _lzssOption.N - _lzssOption.F;
                var stopPos = count + offset;
                var arrayIdx = offset;
                byte[] code_buf = new byte[17];

                InitTree();  /* initialize trees */

                unsafe
                {
                    fixed (byte* pcode_buf = code_buf, ptext_buf = text_buf, pArray = array)
                    {
                        pcode_buf[0] = 0;  /* code_buf[1..16] saves eight units of code, and
                                      code_buf[0] works as eight flags, "1" representing that the unit
                                      is an unencoded letter (1 byte), "0" a position-and-length pair
                                      (2 bytes).  Thus, eight units require at most 16 bytes of code. */

                        for (i = s; i < r; i++)  /* Clear the buffer with any character that will appear often. */
                        {
                            ptext_buf[i] = _lzssOption.FILL;
                        }

                        for (len = 0; len < _lzssOption.F && arrayIdx < stopPos; len++)
                        {
                            ptext_buf[r + len] = pArray[arrayIdx++];  /* Read F bytes into the last F bytes of the buffer */
                        }

                        if (len == 0)
                        {
                            return 0;  /* text of size zero */
                        }
                        for (i = 1; i <= _lzssOption.F; i++)
                        {
                            /* Insert the F strings,
                           each of which begins with one or more 'space' characters.  Note
                           the order in which these strings are inserted.  This way,
                           degenerate trees will be less likely to occur. */
                            InsertNode(r - i);
                        }
                        InsertNode(r);  /* Finally, insert the whole string just read.  The
                                  global variables match_length and match_position are set. */
                        do
                        {
                            if (match_length > len)
                            {
                                match_length = len;  /* match_length
                                                                may be spuriously long near the end of text. */
                            }
                            if (match_length <= _lzssOption.THRESHOLD)
                            {
                                match_length = 1;  /* Not long enough match.  Send one byte. */
                                pcode_buf[0] |= mask;  /* 'send one byte' flag */
                                pcode_buf[code_buf_ptr++] = ptext_buf[r];  /* Send uncoded. */
                            }
                            else
                            {
                                pcode_buf[code_buf_ptr++] = (byte)match_position;
                                pcode_buf[code_buf_ptr++] = (byte)(((match_position >> 4) & 0xf0)
                                                            | (match_length - (_lzssOption.THRESHOLD + 1)));  /* Send position and
                                                                                 length pair. Note match_length > THRESHOLD. */
                            }
                            if ((mask <<= 1) == 0) /* Shift mask left one bit. */
                            {
                                stream.Write(code_buf, 0, code_buf_ptr); /* Send at most 8 units of code together */
                                codesize += code_buf_ptr;
                                pcode_buf[0] = 0; code_buf_ptr = mask = 1;
                            }
                            last_match_length = match_length;
                            for (i = 0; i < last_match_length && arrayIdx < stopPos; i++)
                            {
                                DeleteNode(s);        /* Delete old strings and */
                                c = pArray[arrayIdx++];
                                ptext_buf[s] = c;    /* read new bytes */
                                if (s < _lzssOption.F - 1)
                                {
                                    ptext_buf[s + _lzssOption.N] = c;  /* If the position is near the end of buffer, extend the buffer to make  string comparison easier. */
                                }
                                s = (s + 1) & (_lzssOption.N - 1);
                                r = (r + 1) & (_lzssOption.N - 1);
                                /* Since this is a ring buffer, increment the position
                                   modulo N. */
                                InsertNode(r);    /* Register the string in text_buf[r..r+F-1] */
                            }

                            while (i++ < last_match_length)
                            {    /* After the end of text, */
                                DeleteNode(s);                    /* no need to read, but */
                                s = (s + 1) & (_lzssOption.N - 1);
                                r = (r + 1) & (_lzssOption.N - 1);
                                --len;
                                if (len != 0) InsertNode(r);        /* buffer may not be empty. */
                            }
                        } while (len > 0);    /* until length of string to be processed is zero */

                        if (code_buf_ptr > 1)
                        {        /* Send remaining code. */
                            stream.Write(code_buf, 0, code_buf_ptr);
                            codesize += code_buf_ptr;
                        }
                        stream.Flush();
                        return codesize;
                    }
                }
            }

            public int Decode(byte[] array, int offset, int count, Stream stream)    /* Just the reverse of Encode(). */
            {
                int i, j, k, c;
                var flags = 0U;
                int r = _lzssOption.N - _lzssOption.F;
                var stopPos = count + offset;
                int arrayIdx = offset;
                unsafe
                {
                    fixed (byte* pArray = array, ptext_buf = text_buf)
                    {
                        for (i = 0; i < _lzssOption.N - _lzssOption.F; i++)
                        {
                            ptext_buf[i] = _lzssOption.FILL;
                        }
                        for (; ; )
                        {
                            if (((flags >>= 1) & 256) == 0)
                            {
                                if ((c = stream.ReadByte()) == -1) break;
                                flags = (uint)c | 0xff00U;        /* uses higher byte cleverly to count eight */
                            }
                            if ((flags & 1) != 0)
                            {
                                if ((c = stream.ReadByte()) == -1 || arrayIdx == stopPos) break;
                                pArray[arrayIdx++] = ptext_buf[r++] = (byte)c;
                                r &= (_lzssOption.N - 1);
                            }
                            else
                            {
                                if ((i = stream.ReadByte()) == -1 || (j = stream.ReadByte()) == -1) break;
                                i |= ((j & 0xf0) << 4);
                                j = (j & 0x0f) + _lzssOption.THRESHOLD;
                                for (k = 0; k <= j; k++)
                                {
                                    if (arrayIdx == stopPos) break;
                                    c = ptext_buf[(i + k) & (_lzssOption.N - 1)];
                                    pArray[arrayIdx++] = ptext_buf[r++] = (byte)c;
                                    r &= (_lzssOption.N - 1);
                                }
                            }
                        }
                    }
                }
                return arrayIdx - offset;
            }



        }




    }
}
