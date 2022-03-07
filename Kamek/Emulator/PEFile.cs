using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kamek.Emulator {
	public class PEFile {
		public struct StandardHeader {
			public ushort Machine;
			public ushort NumberOfSections;
			public uint TimeDateStamp;
			public uint PointerToSymbolTable;
			public uint NumberOfSymbols;
			public ushort SizeOfOptionalHeader;
			public ushort Characteristics;
			public OptionalHeader OptionalHeader;

			public static StandardHeader Read(BinaryReader reader) {
				var h = new StandardHeader();

				h.Machine = reader.ReadUInt16();
				h.NumberOfSections = reader.ReadUInt16();
				h.TimeDateStamp = reader.ReadUInt32();
				h.PointerToSymbolTable = reader.ReadUInt32();
				h.NumberOfSymbols = reader.ReadUInt32();
				h.SizeOfOptionalHeader = reader.ReadUInt16();
				h.Characteristics = reader.ReadUInt16();
				h.OptionalHeader = OptionalHeader.Read(reader);

				return h;
			}
		}

		public struct OptionalHeader { // Size: 0x60
			public ushort Magic;
			public byte MajorLinkerVersion, MinorLinkerVersion;
			public uint SizeOfCode;
			public uint SizeOfInitializedData;
			public uint SizeOfUninitializedData;
			public uint AddressOfEntryPoint;
			public uint BaseOfCode, BaseOfData;
			public uint ImageBase;
			public uint SectionAlignment;
			public uint FileAlignment;
			public ushort MajorOperatingSystemVersion, MinorOperatingSystemVersion;
			public ushort MajorImageVersion, MinorImageVersion;
			public ushort MajorSubsystemVersion, MinorSubsystemVersion;
			public uint Win32VersionValue;
			public uint SizeOfImage;
			public uint SizeOfHeaders;
			public uint CheckSum;
			public ushort Subsystem;
			public ushort DllCharacteristics;
			public uint SizeOfStackReserve, SizeOfStackCommit;
			public uint SizeOfHeapReserve, SizeOfHeapCommit;
			public uint LoaderFlags;
			public List<(uint, uint)> RvaAndSizes;

			public uint ImportTableRva => RvaAndSizes[1].Item1;

			public static OptionalHeader Read(BinaryReader reader) {
				var h = new OptionalHeader();

				h.Magic = reader.ReadUInt16();
				if (h.Magic != 0x10B)
					throw new InvalidDataException("Only PE32 is supported");
				h.MajorLinkerVersion = reader.ReadByte(); // 3
				h.MinorLinkerVersion = reader.ReadByte(); // 2
				h.SizeOfCode = reader.ReadUInt32(); // 0x27AA00
				h.SizeOfInitializedData = reader.ReadUInt32(); // 0xDBA00
				h.SizeOfUninitializedData = reader.ReadUInt32(); // 0x79800
				h.AddressOfEntryPoint = reader.ReadUInt32(); // 0x1000
				h.BaseOfCode = reader.ReadUInt32(); // 0x1000
				h.BaseOfData = reader.ReadUInt32(); // 0x27C000
				h.ImageBase = reader.ReadUInt32(); // 0x400000
				h.SectionAlignment = reader.ReadUInt32(); // 0x1000
				h.FileAlignment = reader.ReadUInt32(); // 0x200
				h.MajorOperatingSystemVersion = reader.ReadUInt16(); // 4
				h.MinorOperatingSystemVersion = reader.ReadUInt16(); // 0
				h.MajorImageVersion = reader.ReadUInt16(); // 0
				h.MinorImageVersion = reader.ReadUInt16(); // 0
				h.MajorSubsystemVersion = reader.ReadUInt16(); // 4
				h.MinorSubsystemVersion = reader.ReadUInt16(); // 0
				h.Win32VersionValue = reader.ReadUInt32(); // 0
				h.SizeOfImage = reader.ReadUInt32(); // 0x3D5000
				h.SizeOfHeaders = reader.ReadUInt32(); // 0x400
				h.CheckSum = reader.ReadUInt32(); // 0
				h.Subsystem = reader.ReadUInt16(); // 3
				h.DllCharacteristics = reader.ReadUInt16(); // 0
				h.SizeOfStackReserve = reader.ReadUInt32(); // 0x100000
				h.SizeOfStackCommit = reader.ReadUInt32(); // 0x1000
				h.SizeOfHeapReserve = reader.ReadUInt32(); // 0x100000
				h.SizeOfHeapCommit = reader.ReadUInt32(); // 0x1000
				h.LoaderFlags = reader.ReadUInt32(); // 0
				uint NumberOfRvaAndSizes = reader.ReadUInt32(); // 0x10

				h.RvaAndSizes = new List<(uint, uint)>();
				for (var i = 0; i < NumberOfRvaAndSizes; i++) {
					uint rva = reader.ReadUInt32();
					uint size = reader.ReadUInt32();
					h.RvaAndSizes.Add((rva, size));
				}

				return h;
			}
		}

		public struct Section {
			public string Name;
			public uint VirtualSize, VirtualAddress;
			public uint PointerToRelocations, PointerToLinenumbers;
			public ushort NumberOfRelocations, NumberOfLinenumbers;
			public uint Characteristics;
			public byte[] RawData;

			public static Section Read(BinaryReader reader) {
				var s = new Section();

				s.Name = Encoding.ASCII.GetString(reader.ReadBytes(8)).TrimEnd('\0');
				s.VirtualSize = reader.ReadUInt32();
				s.VirtualAddress = reader.ReadUInt32();
				int SizeOfRawData = reader.ReadInt32();
				uint PointerToRawData = reader.ReadUInt32();
				s.PointerToRelocations = reader.ReadUInt32();
				s.PointerToLinenumbers = reader.ReadUInt32();
				s.NumberOfRelocations = reader.ReadUInt16();
				s.NumberOfLinenumbers = reader.ReadUInt16();
				s.Characteristics = reader.ReadUInt32();

				var savePos = reader.BaseStream.Position;
				if (PointerToRawData > 0 && SizeOfRawData > 0) {
					reader.BaseStream.Position = PointerToRawData;
					s.RawData = reader.ReadBytes(SizeOfRawData);
				}
				reader.BaseStream.Position = savePos;

				return s;
			}
		}

		public StandardHeader Header { get; }
		public List<Section> Sections { get; }

		public PEFile(Stream input) {
			var reader = new BinaryReader(input);

			// Fetch PE header info
			input.Position = 0x3C;
			var peHeaderOffset = reader.ReadInt32();

			input.Position = peHeaderOffset;
			var magic = reader.ReadBytes(4);
			if (magic[0] != 'P' || magic[1] != 'E' || magic[2] != 0 || magic[3] != 0)
				throw new InvalidDataException("Bad PE magic");

			Header = StandardHeader.Read(reader);
			if (Header.Machine != 0x14C)
				throw new InvalidDataException("Only x86 is supported");

			Sections = new List<Section>();
			for (var i = 0; i < Header.NumberOfSections; i++) {
				Sections.Add(Section.Read(reader));
			}
		}
	}
}
