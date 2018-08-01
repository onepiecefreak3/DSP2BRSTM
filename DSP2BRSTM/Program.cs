using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace DSP2BRSTM
{
    class Program
    {
        public static void ExitWithError(string message)
        {
            Console.WriteLine(message);
            Environment.Exit(0);
        }

        void PrintHelp()
        {
            Console.WriteLine("Usage:\r\n" +
                $"{Path.GetFileName(Assembly.GetEntryAssembly().Location)} <mode> <List of DSP's>\r\n" +
                $"\r\n" +
                $"Available modes:\r\n" +
                $"-wav\tExports every named DSP to a wav\r\n" +
                $"-n\tExports every named dsp to brstm\r\n" +
                $"-m\tExports every named dsp into one multi-channel brstm\r\n" +
                $"-wavm\tExports every named dsp into one multi-channel wav");
        }

        private static void ValidateArgs(string[] args)
        {
            if (args.Length <= 0)
                ExitWithError("No arguments given");
            if (args[0] != "-m" && args[0] != "-n" && args[0] != "-wav" && args[0] != "-wavm")
                ExitWithError($"Mode {args[0]} unknown.");
        }
        static void Main(string[] args)
        {
            ValidateArgs(args);
            var mode = args[0];
            var dsps = args.Skip(1).Take(args.Length - 1).Select(f => new DSP(f)).ToList();

            Convert(dsps, mode);
        }

        private static void Convert(List<DSP> dsps, string mode)
        {
            switch (mode)
            {
                case "-wav":
                    ToWav(dsps);
                    break;
                case "-wavm":
                    MergeWav(dsps);
                    break;
                case "-n":
                    ToBRSTM(dsps);
                    break;
                case "-m":
                    MergeBRSTM(dsps);
                    break;
            }
        }

        private static void ToWav(List<DSP> dsps)
        {
            for (int i = 0; i < dsps.Count; i++)
                new WAV(1, dsps[i].GetSampleRate()).Write(File.Create(Path.Combine(Path.GetDirectoryName(dsps[i].GetFilePath()), $"export{i}.wav")), new List<short[]> { dsps[i].Decode() });
        }

        private static void MergeWav(List<DSP> dsps)
        {
            if (dsps.Select(d => d.GetSampleRate()).Distinct().Count() > 1)
                ExitWithError($"All DSPs need to have the same sample rate to be merged.");

            new WAV(dsps.Count, dsps[0].GetSampleRate())
                .Write(File.Create(Path.Combine(Path.GetDirectoryName(dsps[0].GetFilePath()), $"export.wav")), dsps.Select(d => d.Decode()).ToList());
        }

        private static void ToBRSTM(List<DSP> dsps)
        {
            var brstms = dsps.Select(d => new BRSTM(IO.ByteOrder.BigEndian, 1, 0, BRSTM.TrackType.Default, 1, d.GetSampleRate())).ToList();

            for (int i = 0; i < brstms.Count; i++)
                brstms[i].Write(
                    File.Create(Path.Combine(Path.GetDirectoryName(dsps[i].GetFilePath()),
                    $"export{i}.brstm")),
                    new List<DSP> { dsps[i] }
                    );
        }

        private static void MergeBRSTM(List<DSP> dsps)
        {
            if (dsps.Select(d => d.GetSampleRate()).Distinct().Count() > 1)
                ExitWithError($"All DSPs need to have the same sample rate to be merged.");

            new BRSTM(IO.ByteOrder.BigEndian, 1, 0, BRSTM.TrackType.SuperSmashBros, dsps.Count, dsps[0].GetSampleRate())
                .Write(File.Create(Path.Combine(Path.GetDirectoryName(dsps[0].GetFilePath()), $"export.brstm")), dsps);
        }
    }
}
