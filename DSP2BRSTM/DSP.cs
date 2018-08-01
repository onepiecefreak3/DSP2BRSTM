using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DSP2BRSTM.IO;

namespace DSP2BRSTM
{
    public class DSP : IDisposable
    {
        class Header
        {
            public int sampleCount;
            public int nibbleCount;
            public int sampleRate;
            public short loopFlag;
            public short format;
            public int loopStart;
            public int loopEnd;
            public int currentAddress;
            [FixedLength(0x10)]
            public short[] coefs;
            public short gain;
            public short initPred;
            public short history1;
            public short history2;
            public short loopPred;
            public short loopHist1;
            public short loopHist2;
            [FixedLength(0xB)]
            public short[] reserved;
        }

        private string _filePath;
        private Stream _dsp;

        private int _headerLength = 0x2A + 0x10 * 2 + 0xB * 2;
        private Header _dspHeader;

        private int _frameLength = 8;

        public DSP(string dsp)
        {
            if (!File.Exists(dsp))
                Program.ExitWithError($"File {dsp} doesn't exist");
            _filePath = dsp;
            _dsp = File.OpenRead(dsp);

            using (var br = new BinaryReaderX(_dsp, true, ByteOrder.BigEndian))
                _dspHeader = br.ReadStruct<Header>();
        }

        public short[] Decode()
        {
            short hist1 = _dspHeader.history1;
            short hist2 = _dspHeader.history2;
            short[] coefs = _dspHeader.coefs;
            List<short> result = new List<short>();

            using (var br = new BinaryReaderX(_dsp, true))
            {
                br.BaseStream.Position = _headerLength;
                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    // Each frame, we need to read the header byte and use it to set the scale and coefficient values:
                    byte head = br.ReadByte();

                    ushort scale = (ushort)(1 << (head & 0xF));
                    byte coefIndex = (byte)(head >> 4);
                    short coef1 = coefs[2 * coefIndex];
                    short coef2 = coefs[2 * coefIndex + 1];

                    // 7 bytes per frame
                    for (uint i = 0; i < 7; i++)
                    {
                        byte b = br.ReadByte();

                        // 2 samples per byte
                        for (uint s = 0; s < 2; s++)
                        {
                            sbyte adpcmNibble = (s == 0) ? GetHighNibble(b) : GetLowNibble(b);
                            short sample = Clamp(((adpcmNibble * scale) << 11) + 1024 + ((coef1 * hist1) + (coef2 * hist2)) >> 11, -32768, 32767);

                            hist2 = hist1;
                            hist1 = sample;
                            result.Add(sample);
                        }
                    }
                }
            }

            return result.ToArray();
        }

        sbyte[] NibbleToSByte = { 0, 1, 2, 3, 4, 5, 6, 7, -8, -7, -6, -5, -4, -3, -2, -1 };
        sbyte GetLowNibble(byte value)
        {
            return NibbleToSByte[value & 0xF];
        }
        sbyte GetHighNibble(byte value)
        {
            return NibbleToSByte[value >> 4];
        }

        short Clamp(int value, int minValue, int maxValue)
        {
            if (value < minValue) value = minValue;
            if (value > maxValue) value = maxValue;
            return (short)value;
        }

        public short[] GetCoefficients()
        {
            return _dspHeader.coefs;
        }

        public int GetSampleCount()
        {
            return _dspHeader.sampleCount;
        }

        public short GetHistory1()
        {
            return _dspHeader.history1;
        }

        public short GetHistory2()
        {
            return _dspHeader.history2;
        }

        public byte[] GetEncodedData()
        {
            using (var br = new BinaryReaderX(_dsp, true))
            {
                br.BaseStream.Position = _headerLength;
                return br.ReadBytes(_dspHeader.nibbleCount / 2);
            }
        }

        public int GetSampleRate()
        {
            return _dspHeader.sampleRate;
        }

        public string GetFilePath()
        {
            return _filePath;
        }

        public int GetFrameSize()
        {
            return _frameLength;
        }

        public bool IsLooped()
        {
            return _dspHeader.loopFlag > 0;
        }

        public int GetLoopStart()
        {
            return _dspHeader.loopStart;
        }

        public short GetLoopHist1()
        {
            return _dspHeader.loopHist1;
        }

        public short GetLoopHist2()
        {
            return _dspHeader.loopHist2;
        }

        public void Dispose()
        {
            _dsp.Dispose();
        }
    }
}
