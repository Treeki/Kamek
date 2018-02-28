using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kamek
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Kamek 2.0 by Treeki");
            Console.WriteLine();

            // Parse the command line arguments and do cool things!
            var modules = new List<Elf>();
            uint? baseAddress = null;
            string outputKamekPath = null, outputRiivPath = null, outputGeckoPath = null, outputCodePath = null;
            string inputDolPath = null, outputDolPath = null;
            var externals = new Dictionary<string, uint>();
            VersionInfo versions = null;

            foreach (var arg in args)
            {
                if (arg.StartsWith("-"))
                {
                    if (arg == "-dynamic")
                        baseAddress = null;
                    else if (arg.StartsWith("-static=0x"))
                        baseAddress = uint.Parse(arg.Substring(10), System.Globalization.NumberStyles.HexNumber);
                    else if (arg.StartsWith("-output-kamek="))
                        outputKamekPath = arg.Substring(14);
                    else if (arg.StartsWith("-output-riiv="))
                        outputRiivPath = arg.Substring(13);
                    else if (arg.StartsWith("-output-gecko="))
                        outputGeckoPath = arg.Substring(14);
                    else if (arg.StartsWith("-output-code="))
                        outputCodePath = arg.Substring(13);
                    else if (arg.StartsWith("-input-dol="))
                        inputDolPath = arg.Substring(11);
                    else if (arg.StartsWith("-output-dol="))
                        outputDolPath = arg.Substring(12);
                    else if (arg.StartsWith("-externals="))
                        ReadExternals(externals, arg.Substring(11));
                    else if (arg.StartsWith("-versions="))
                        versions = new VersionInfo(arg.Substring(10));
                    else
                        Console.WriteLine("warning: unrecognised argument: {0}", arg);
                }
                else
                {
                    Console.WriteLine("adding {0} as object..", arg);
                    using (var stream = new FileStream(arg, FileMode.Open))
                    {
                        modules.Add(new Elf(stream));
                    }
                }
            }


            // We need a default VersionList for the loop later
            if (versions == null)
                versions = new VersionInfo();


            // Can we build a thing?
            if (modules.Count == 0)
            {
                Console.WriteLine("no input files specified");
                return;
            }
            if (outputKamekPath == null && outputRiivPath == null && outputGeckoPath == null && outputCodePath == null && outputDolPath == null)
            {
                Console.WriteLine("no output path(s) specified");
                return;
            }
            if (outputDolPath != null && inputDolPath == null)
            {
                Console.WriteLine("input dol path not specified");
                return;
            }


            foreach (var version in versions.Mappers)
            {
                Console.WriteLine("linking version {0}...", version.Key);

                var linker = new Linker(version.Value);
                foreach (var module in modules)
                    linker.AddModule(module);

                if (baseAddress.HasValue)
                    linker.LinkStatic(baseAddress.Value, externals);
                else
                    linker.LinkDynamic(externals);

                var kf = new KamekFile();
                kf.LoadFromLinker(linker);
                if (outputKamekPath != null)
                    File.WriteAllBytes(outputKamekPath.Replace("$KV$", version.Key), kf.Pack());
                if (outputRiivPath != null)
                    File.WriteAllText(outputRiivPath.Replace("$KV$", version.Key), kf.PackRiivolution());
                if (outputGeckoPath != null)
                    File.WriteAllText(outputGeckoPath.Replace("$KV$", version.Key), kf.PackGeckoCodes());
                if (outputCodePath != null)
                    File.WriteAllBytes(outputCodePath.Replace("$KV$", version.Key), kf.CodeBlob);

                if (outputDolPath != null)
                {
                    var dol = new Dol(new FileStream(inputDolPath, FileMode.Open));
                    kf.InjectIntoDol(dol);

                    var outpath = outputDolPath.Replace("$KV$", version.Key);
                    using (var outStream = new FileStream(outpath, FileMode.Create))
                    {
                        dol.Write(outStream);
                    }
                }
            }
        }

        private static void ReadExternals(Dictionary<string, uint> dict, string path)
        {
            var commentRegex = new Regex(@"^\s*#");
            var emptyLineRegex = new Regex(@"^\s*$");
            var assignmentRegex = new Regex(@"^\s*([a-zA-Z0-9_<>\$]+)\s*=\s*0x([a-fA-F0-9]+)\s*(#.*)?$");

            foreach (var line in File.ReadAllLines(path))
            {
                if (emptyLineRegex.IsMatch(line))
                    continue;
                if (commentRegex.IsMatch(line))
                    continue;

                var match = assignmentRegex.Match(line);
                if (match.Success)
                {
                    dict[match.Groups[1].Value] = uint.Parse(match.Groups[2].Value, System.Globalization.NumberStyles.HexNumber);
                }
                else
                {
                    Console.WriteLine("unrecognised line in externals file: {0}", line);
                }
            }
        }
    }
}
