using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DSP2BRSTM.IO;

namespace DSP2BRSTM
{
    public class BRSTM
    {
        class Header
        {
            public Header(ByteOrder byteOrder, int major, int minor)
            {
                bom = byteOrder;
                this.major = (byte)major;
                this.minor = (byte)minor;
            }
            [FixedLength(4)]
            public string magic = "RSTM";
            [Endianness(ByteOrder = ByteOrder.LittleEndian)]
            public ByteOrder bom;
            public byte major;
            public byte minor;
            public int fileSize;                //??
            public short headerSize = 0x40;
            public short chunkCount = 2;
            public int HEADChunkOffset = 0x40;
            public int HEADChunkSize;
            public int ADPCOffset;
            public int ADPCSize;
            public int DATAOffset;
            public int DATASize;
        }

        private int _HEADSize = 0x20;
        class HEADChunkHeader
        {
            [FixedLength(4)]
            public string magic = "HEAD";
            public int HEADLength;
            public int marker1 = 0x01000000;
            public int chunk1Offset;
            public int marker2 = 0x01000000;
            public int chunk2Offset;
            public int marker3 = 0x01000000;
            public int chunk3Offset;
        }

        private int _HEADChunk1Size = 0x34;
        class HEADChunk1
        {
            public Codec codec = Codec.ADPCM4bit;
            public byte loopFlag = 0;
            public byte channelCount;
            public byte padding = 0;
            public short sampleRate;
            public short padding1 = 0;
            public int loopStart = 0;
            public int sampleCount;
            public int ADPCMOffAbs;
            public int blockCount;
            public int blockSize = 0x2000;
            public int samplesPerBlock = 0x3800;
            public int finalBlockSizeWithoutPad;
            public int samplesInFinalBlock;
            public int finalBlockSizeWithPad;
            public int samplesPerADPCEntry = 0x3800;
            public int BytesPerADPCEntry = 4;

            public enum Codec : byte
            {
                PCM8bit = 0,
                PCM16bit,
                ADPCM4bit
            }
        }

        private int _HC2HeadSize = 4;
        private int _HC2OffsetEntrySize = 8;
        private int _track0Size = 4;
        private int _track1Size = 12;
        public enum TrackType : byte
        {
            SuperSmashBros = 0,
            Default = 1
        }
        class HEADChunk2
        {
            public byte TrackCount = 1;
            public TrackType TrackType = TrackType.Default;
            public short padding = 0;

            public class OffsetEntry
            {
                public byte unk1 = 0x01;
                public TrackType trackDesc = TrackType.Default;
                public short padding = 0;
                public int trackDescOffset;
            }

            public class TrackType0
            {
                public byte ChannelCount;
                public byte LeftChannelID = 0;
                public byte RightChannelID = 0;
                public byte padding = 0;
            }
            public class TrackType1
            {
                public byte Volume = 0x2F;
                public byte Panning = 0x3F;
                public short padding = 0;
                public int padding2 = 0;
                public byte ChannelCount;
                public byte LeftChannelID = 0;
                public byte RightChannelID = 0;
                public byte padding3 = 0;
            }
        }

        private int _HC3HeadSize = 4;
        private int _HC3OffsetEntrySize = 8;
        private int _HC3ADPCMChInfoSize = 0x38;
        class HEADChunk3
        {
            public byte channelCount;
            [FixedLength(3)]
            public byte[] padding = new byte[3];

            public class OffsetEntry
            {
                public int marker = 0x01000000;
                public int channelOffset;
            }

            public class ADPCMChannelInfo
            {
                public int marker = 0x01000000;
                public int channelCoefsOffset;
                [FixedLength(0x10)]
                public short[] coefs;
                public short gain = 0;
                public short predictor = 0;
                public short historySample1 = 0;
                public short historySample2 = 0;
                public short loopPredictor = 0;
                public short loopHistorySample1 = 0;
                public short loopHistorySample2 = 0;
                public short padding = 0;
            }
        }

        private int _ADPCSize = 8;
        class ADPCHeader
        {
            [FixedLength(4)]
            public string magic = "ADPC";
            public int ADPCLength;
        }

        class ADPCEntry
        {
            public short channelHistorySample1;
            public short channelHistorySample2;
        }

        private int _DATASize = 0x20;
        class DATAHeader
        {
            [FixedLength(4)]
            public string magic = "DATA";
            public int DATALength;
            public int unk1 = 0x18;
        }

        private ByteOrder _byteOrder;
        private byte _major;
        private byte _minor;
        private TrackType _trackType;
        private int _channelCount;
        private int _sampleRate;

        public BRSTM(ByteOrder byteOrder, byte major, byte minor, TrackType trackType, int channelCount, int sampleRate)
        {
            _byteOrder = byteOrder;
            _major = major;
            _minor = minor;
            _trackType = trackType;
            _channelCount = channelCount;
            _sampleRate = sampleRate;
        }

        public void Write(Stream output, List<DSP> dsps)
        {
            int Align(int input, int align) => (input + (align - 1)) & ~(align - 1);

            var header = new Header(_byteOrder, _major, _minor);

            var HEAD = new HEADChunkHeader();

            var HEADChunk1 = new HEADChunk1();
            HEADChunk1.channelCount = (byte)_channelCount;
            HEADChunk1.sampleRate = (short)_sampleRate;
            HEADChunk1.loopFlag = (byte)(dsps[0].IsLooped() ? 1 : 0);
            HEADChunk1.loopStart = dsps[0].GetLoopStart();
            HEADChunk1.sampleCount = dsps[0].GetSampleCount();
            HEADChunk1.blockCount = dsps[0].GetEncodedData().Length / HEADChunk1.blockSize + ((dsps[0].GetEncodedData().Length % HEADChunk1.blockSize > 0) ? 1 : 0);
            HEADChunk1.finalBlockSizeWithoutPad = dsps[0].GetEncodedData().Length % HEADChunk1.blockSize;
            HEADChunk1.samplesInFinalBlock = dsps[0].GetEncodedData().Length % HEADChunk1.blockSize / 8 * 14;
            HEADChunk1.finalBlockSizeWithPad = Align(dsps[0].GetEncodedData().Length % HEADChunk1.blockSize, 0x20);

            var HEADChunk2 = new HEADChunk2();
            HEADChunk2.TrackType = _trackType;
            var HEADChunk2Size = 4 + HEADChunk2.TrackCount * 8 + HEADChunk2.TrackCount * ((HEADChunk2.TrackType == TrackType.SuperSmashBros) ? _track0Size : _track1Size);
            var HEADChunk3 = new HEADChunk3();
            HEADChunk3.channelCount = (byte)_channelCount;
            var HEADChunk3Size = 8 + _channelCount * 8 + _channelCount * (8 + 0x20 + 0x10);

            var ADPC = new ADPCHeader();
            ADPCEntry[] adpcEntries = new ADPCEntry[_channelCount];
            for (int i = 0; i < _channelCount; i++)
                adpcEntries[i] = new ADPCEntry() { channelHistorySample1 = 0, channelHistorySample2 = 0 };

            var DATA = new DATAHeader();

            using (var bw = new BinaryWriterX(output, true, _byteOrder))
            {
                bw.BaseStream.Position = header.headerSize + _HEADSize;
                bw.WriteAlignment(0x20);
                var rel = bw.BaseStream.Position;

                //HEADChunk1
                HEAD.chunk1Offset = (int)bw.BaseStream.Position - header.headerSize - 8;
                bw.BaseStream.Position += _HEADChunk1Size;
                //bw.WriteStruct(HEADChunk1);
                //bw.WriteAlignment(0x20);

                //HEADChunk2
                HEAD.chunk2Offset = (int)bw.BaseStream.Position - header.headerSize - 8;
                bw.WriteStruct(HEADChunk2);
                if (HEADChunk2.TrackType == TrackType.SuperSmashBros)
                {
                    bw.WriteMultiple(Enumerable.Range(0, HEADChunk2.TrackCount).Select(i => new HEADChunk2.OffsetEntry
                    {
                        trackDesc = _trackType,
                        trackDescOffset = HEAD.chunk2Offset + _HC2HeadSize + HEADChunk2.TrackCount * _HC2OffsetEntrySize + i * _track0Size
                    }));
                    bw.WriteMultiple(Enumerable.Range(0, HEADChunk2.TrackCount).Select(i => new HEADChunk2.TrackType0
                    {
                        ChannelCount = (byte)_channelCount,
                        RightChannelID = (byte)((dsps.Count > 1) ? 1 : 0)
                    }));
                }
                else
                {
                    bw.WriteMultiple(Enumerable.Range(0, HEADChunk2.TrackCount).Select(i => new HEADChunk2.OffsetEntry
                    {
                        trackDescOffset = HEAD.chunk2Offset + _HC2HeadSize + HEADChunk2.TrackCount * _HC2OffsetEntrySize + i * _track1Size
                    }));
                    bw.WriteMultiple(Enumerable.Range(0, HEADChunk2.TrackCount).Select(i => new HEADChunk2.TrackType1
                    {
                        ChannelCount = (byte)_channelCount,
                        RightChannelID = (byte)((dsps.Count > 1) ? 1 : 0)
                    }));
                }
                //bw.WriteAlignment(0x20);

                //HEADChunk3
                HEAD.chunk3Offset = (int)bw.BaseStream.Position - header.headerSize - 8;
                var offsetEntries = Enumerable.Range(0, dsps.Count).Select(i => new HEADChunk3.OffsetEntry { channelOffset = HEAD.chunk3Offset + _HC3HeadSize + dsps.Count * _HC3OffsetEntrySize + i * _HC3ADPCMChInfoSize }).ToArray();
                bw.WriteStruct(HEADChunk3);
                bw.WriteMultiple(offsetEntries);
                var channelInfo = dsps.Select((d, i) => new HEADChunk3.ADPCMChannelInfo
                {
                    channelCoefsOffset = ((int)bw.BaseStream.Position - header.headerSize - 8) + 8 + i * _HC3ADPCMChInfoSize,
                    coefs = d.GetCoefficients(),
                    loopHistorySample1 = d.GetLoopHist1(),
                    loopHistorySample2 = d.GetLoopHist2(),
                });
                bw.WriteMultiple(channelInfo);
                bw.WriteAlignment(0x20);

                //HEAD
                HEAD.HEADLength = header.HEADChunkSize = (int)bw.BaseStream.Position - header.headerSize;
                var bk = bw.BaseStream.Position;
                bw.BaseStream.Position = rel - _HEADSize;
                bw.WriteStruct(HEAD);
                bw.BaseStream.Position = bk;

                //ADPC
                header.ADPCOffset = (int)bw.BaseStream.Position;
                ADPC.ADPCLength = Align((HEADChunk1.blockCount - 1) * 4 * _channelCount + 0x10, 0x20);
                bw.WriteStruct(ADPC);

                //ADPC Entries
                bw.Write(GetADPCData(dsps, HEADChunk1.blockCount));

                bw.BaseStream.Position = header.ADPCOffset + ADPC.ADPCLength;
                bw.WriteAlignment(0x20);
                header.ADPCSize = ADPC.ADPCLength;

                //DATA
                header.DATAOffset = (int)bw.BaseStream.Position;
                DATA.DATALength = _DATASize + dsps.Sum(d => Align(d.GetEncodedData().Length, 0x20));
                bw.WriteStruct(DATA);
                bw.WriteAlignment(0x20);

                //Actual audio data
                HEADChunk1.ADPCMOffAbs = (int)bw.BaseStream.Position;
                if (_channelCount == 1)
                {
                    foreach (var data in dsps.Select(d => d.GetEncodedData()))
                        bw.Write(data);
                }
                else
                {
                    for (int i = 0; i < dsps.Count; i += _channelCount)
                    {
                        bw.Write(Interleave(dsps.Select(d => d.GetEncodedData()).Skip(i).Take(_channelCount).ToList(), HEADChunk1.blockSize, HEADChunk1.blockCount));
                    }
                }
                bw.WriteAlignment(0x20);
                header.DATASize = (int)bw.BaseStream.Position - header.DATAOffset;
                header.fileSize = (int)bw.BaseStream.Length;

                //HEADChunk1
                bw.BaseStream.Position = HEAD.chunk1Offset + header.headerSize + 8;
                bw.WriteStruct(HEADChunk1);

                //Header
                bw.BaseStream.Position = 0;
                bw.WriteStruct(header);
            }
        }

        private byte[] Interleave(List<byte[]> audio, int blockSize, int blockCount)
        {
            int Align(int input, int align) => (input + (align - 1)) & ~(align - 1);

            var channelCount = audio.Count;

            var result = new byte[channelCount * Align(audio[0].Length, 0x20)];
            var offset = 0;
            for (int i = 0; i < blockCount; i++)
                for (int j = 0; j < channelCount; j++)
                {
                    var data = audio[j].GetBytes(i * blockSize, blockSize);
                    data.CopyTo(result, offset);
                    if (i < blockCount - 1)
                        offset += blockSize;
                    else
                        offset += Align(data.Length, 0x20);
                }

            return result;
        }

        private byte[] GetADPCData(List<DSP> dsps, int blockCount)
        {
            var decodedData = new short[2][];
            for (int i = 0; i < dsps.Count; i++)
                decodedData[i] = dsps[i].Decode();

            var ms = new MemoryStream();
            using (var bw = new BinaryWriterX(ms, true, _byteOrder))
                for (int i = 0; i < blockCount; i++)
                    for (int j = 0; j < dsps.Count; j++)
                        if (i == 0)
                        {
                            bw.Write((short)0);
                            bw.Write((short)0);
                        }
                        else
                        {
                            bw.Write(decodedData[j][i * 0x3800 - 1]);
                            bw.Write(decodedData[j][i * 0x3800 - 2]);
                        }

            return ms.ToArray();
        }
    }
}
