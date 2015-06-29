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
            DemoMetadata();
            //var elf = new Elf(new FileStream(@"D:\repos\oldNewer\objects\___src_bossCaptainBowser_cpp.o", FileMode.Open));
            //Console.ReadKey();
            //AttemptNewer();
        }

        static void DemoMetadata()
        {
            var externals = new Dictionary<string, uint>();
            externals["foo__Fv"] = 0x80808080;
            externals["baz"] = 0x91111110;

            var elf = new Elf(new FileStream(@"D:\repos\NewKamek\preproc_demo.o", FileMode.Open));

            Console.WriteLine("--- DYNAMIC LINK ---");
            var linker = new Linker();
            linker.AddModule(elf);
            linker.LinkDynamic(externals);
            File.WriteAllBytes(@"D:\repos\NewKamek\preproc_code.bin", linker.Output);

            Console.WriteLine("--- STATIC LINK ---");
            var linker2 = new Linker();
            linker2.AddModule(elf);
            linker2.LinkStatic(0x80001800, externals);
            File.WriteAllBytes(@"D:\repos\NewKamek\preproc_code_static.bin", linker2.Output);

            Console.ReadKey();
        }

        static void AttemptNewer()
        {
            string basePath = @"D:\repos\oldNewer";
            string mapPath = Path.Combine(basePath, "kamek_pal.x");
            string objDir = Path.Combine(basePath, "objects");

            var externals = new Dictionary<string, uint>();
            externals["BgTexMng__LoadAnimTile__FPvisPcPcc"] = 0x80087B60;
            externals["__dt__20daJrClownForPlayer_cFv"] = 0x80810540;
            externals["WriteParsedStringToTextBox__FPQ34nw4r3lyt7TextBoxPCwiPA1_19@class$410newer_cppPQ27dScript5Res_c"] = 0x800C9F70;
            externals["CurrentStartedArea"] = 0x80315E96;
            externals["CurrentStartedEntrance"] = 0x80315E97;
            var regexp = new Regex(@"^\s*(?<name>[a-zA-Z0-9_]+)\s*=\s*0x(?<address>[a-fA-F0-9]+);\s*$");

            foreach (string line in File.ReadAllLines(mapPath))
            {
                var match = regexp.Match(line);
                if (match.Success)
                {
                    externals[match.Groups["name"].Value] = uint.Parse(match.Groups["address"].Value, System.Globalization.NumberStyles.HexNumber);
                }
                else
                {
                    if (line.Trim().Length > 0)
                        Console.WriteLine(line);
                }
            }

            Console.ReadKey();

            var linker = new Linker();

            foreach (string objPath in Directory.GetFiles(objDir))
            {
                Console.WriteLine(objPath);

                using (var stream = new FileStream(objPath, FileMode.Open))
                {
                    linker.AddModule(new Elf(stream));
                }
            }

            linker.LinkDynamic(externals);

            // Output in OLD FORMAT for now
            var bigBlob = new byte[linker.Output.Length];
            Array.Copy(linker.Output, bigBlob, linker.Output.Length);
            File.WriteAllBytes(Path.Combine(basePath, "csBuiltCode.bin"), bigBlob);

            // this is missing a lot of crap, but yeah
            using (var stm = new FileStream(Path.Combine(basePath, "csBuiltRelocs.bin"), FileMode.Create))
            {
                using (var bw = new BinaryWriter(stm))
                {
                    bw.Write(new byte[] { (byte)'N', (byte)'e', (byte)'w', (byte)'e', (byte)'r', (byte)'R', (byte)'E', (byte)'L' });
                    bw.WriteBE((uint)(12 + linker.Fixups.Count * 8));

                    uint id = 0;
                    foreach (var fixup in linker.Fixups)
                    {
                        uint a = ((uint)fixup.type << 24) | id;
                        uint b = fixup.source.Addr;
                        bw.WriteBE(a);
                        bw.WriteBE(b);
                        ++id;
                    }
                    foreach (var fixup in linker.Fixups)
                    {
                        bw.WriteBE((uint)fixup.dest.Addr);
                    }
                }
            }

            Console.WriteLine("Link complete!");
            Console.ReadKey();
        }


        static void OriginalTest()
        {
            var externals = new Dictionary<string, uint>();
            externals["original_function__Fi"] = 0x80123454;
            externals["__ct__12ComplexThingFPCc"] = 0x90191234;
            externals["__dt__12ComplexThingFv"] = 0x90191300;
            externals["__register_global_object"] = 0x99900000;

            var elf1 = new Elf(new FileStream("D:\\repos\\NewKamek\\demo.o", FileMode.Open));
            var elf2 = new Elf(new FileStream("D:\\repos\\NewKamek\\asm.o", FileMode.Open));
            var linker = new Linker();
            linker.AddModule(elf1);
            linker.AddModule(elf2);
            linker.LinkDynamic(externals);
            //linker.LinkStatic(0x80001800, externals);

            // extract all symbols
            /*foreach (var section in (from x in elf.Sections
                                     where x.sh_type == Elf.ElfSection.Type.SHT_SYMTAB
                                     select x))
            {
                // we must have a string table
                uint strTabIdx = section.sh_link;
                if (strTabIdx < 0 || strTabIdx >= elf.Sections.Count)
                    throw new InvalidDataException("Symbol table is not linked to a string table");

                var strTab = elf.Sections[(int)strTabIdx];
                if (strTab.sh_type != Elf.ElfSection.Type.SHT_STRTAB)
                    throw new InvalidDataException("Symbol table is not linked to a string table");

                // ok, now iterate through every string!
                var reader = new BinaryReader(new MemoryStream(section.data));
                
                for (int i = 0; i < (section.data.Length / 16); i++)
                {
                    uint st_name = reader.ReadBigUInt32();
                    uint st_value = reader.ReadBigUInt32();
                    uint st_size = reader.ReadBigUInt32();
                    byte st_info = reader.ReadByte();
                    byte st_other = reader.ReadByte();
                    ushort st_shndx = reader.ReadBigUInt16();

                    string name = Util.ExtractNullTerminatedString(strTab.data, (int)st_name);
                    Console.WriteLine(name);
                }
            }*/

            Console.ReadKey();
        }
    }
}
