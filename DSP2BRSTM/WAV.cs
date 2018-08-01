using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DSP2BRSTM
{
    public class WAV
    {
        public uint RIFFMagic = 0x46464952;
        //FileSize
        public uint RIFFType = 0x45564157;

        public uint fmtTag = 0x20746D66;
        public int chunkSize = 0x10;
        public short formatTag = 0x1;
        public short channelCount;
        public int sampleRate;
        //avgBytesPerSec;
        //blockAlign;
        public short bitsPerSample = 0x10;

        public uint dataHead = 0x61746164;

        public WAV(int channelCount, int sampleRate)
        {
            this.channelCount = (short)channelCount;
            this.sampleRate = sampleRate;
        }

        public void Write(Stream file, List<short[]> audioData)
        {
            if (audioData.Count != channelCount)
                Program.ExitWithError($"WAV: Number of DSPs and channelCount need to be equivalent.");

            using (var bw = new BinaryWriter(file))
            {
                bw.Write(RIFFMagic);
                bw.Write(audioData.Sum(a => a.Length) * 2 + 0x28);
                bw.Write(RIFFType);

                bw.Write(fmtTag);
                bw.Write(chunkSize);
                bw.Write(formatTag);
                bw.Write(channelCount);
                bw.Write(sampleRate);
                bw.Write(sampleRate * 2);
                bw.Write((short)(channelCount * 2));
                bw.Write(bitsPerSample);

                bw.Write(dataHead);
                bw.Write(audioData.Sum(a => a.Length) * 2);

                if (channelCount == 1)
                {
                    foreach (var pcm in audioData[0])
                        bw.Write(pcm);
                }
                else
                {
                    bw.Write(Interleave(audioData));
                }
            }
        }

        private byte[] Interleave(List<short[]> audioData)
        {
            var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, Encoding.ASCII, true))
            {
                for (int i = 0; i < audioData[0].Length; i++)
                    for (int j = 0; j < audioData.Count; j++)
                        bw.Write(audioData[j][i]);
            }
            return ms.ToArray();
        }
    }
}
