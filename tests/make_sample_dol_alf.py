import dataclasses
from pathlib import Path
import struct


@dataclasses.dataclass
class Section:
    addr: int
    data: bytes
    is_bss: bool = False

@dataclasses.dataclass
class Symbol:
    mangled_name: bytes
    demangled_name: bytes
    addr: int
    size: int
    is_data: bool
    section_id: int
    unk_10: int

def pad(data: bytes, size: int) -> bytes:
    return data + b'\0' * (size - len(data))


text_sections = [
    Section(0x80000000, pad(b'.text0', 0x1000)),
    Section(0x80001000, pad(b'.text1', 0x1000)),
    Section(0x81000000, pad(b'.text2', 0x1000)),
    Section(0x81001000, pad(b'.text3', 0x1000)),
]
data_sections = [
    Section(0x80002000, pad(b'.data0', 0x1000)),
    Section(0x80003000, pad(b'.data1', 0x1000)),
    Section(0x81002000, pad(b'.data2', 0x1000)),
    Section(0x81003000, pad(b'.data3', 0x1000)),
]
symbols = [
    Symbol(b'mangled1', b'demangled1', 0x80000000, 4, False, 1, 0),
    Symbol(b'mangled2', b'demangled2', 0x80002000, 4, True, 5, 1),
]
bss_section = Section(0x80008000, b'\0' * 0x1000, True)
entrypoint = 0x80000000


def make_dol() -> bytes:
    dol_data = bytearray(256)

    def write_header_u32(offs: int, value: int) -> None:
        struct.pack_into('>I', dol_data, offs, value)

    write_header_u32(0xd8, bss_section.addr)
    write_header_u32(0xdc, len(bss_section.data))
    write_header_u32(0xe0, entrypoint)

    for i, sec in enumerate(text_sections):
        sec_offset = len(dol_data)
        dol_data += sec.data
        assert len(dol_data) % 4 == 0
        write_header_u32(0x00 + i * 4, sec_offset)
        write_header_u32(0x48 + i * 4, sec.addr)
        write_header_u32(0x90 + i * 4, len(sec.data))

    for i, sec in enumerate(data_sections):
        sec_offset = len(dol_data)
        dol_data += sec.data
        assert len(dol_data) % 4 == 0
        write_header_u32(0x1c + i * 4, sec_offset)
        write_header_u32(0x64 + i * 4, sec.addr)
        write_header_u32(0xac + i * 4, len(sec.data))

    return dol_data


def make_alf(version: int) -> bytes:
    alf_data = bytearray()

    def write_u32(value: int) -> None:
        nonlocal alf_data
        alf_data += struct.pack('<I', value)

    def write_u32_to(offs: int, value: int) -> None:
        struct.pack_into('<I', alf_data, offs, value)

    sections = [*text_sections, *data_sections, bss_section]
    sections.sort(key=lambda s: s.addr)

    write_u32(0x464F4252)
    write_u32(version)
    write_u32(entrypoint)
    write_u32(len(sections))

    for section in sections:
        write_u32(section.addr)
        write_u32(0 if section.is_bss else len(section.data))
        write_u32(len(section.data))
        if not section.is_bss:
            alf_data += section.data

    table_size_offs = len(alf_data)
    write_u32(0)
    write_u32(len(symbols))
    for symbol in symbols:
        write_u32(len(symbol.mangled_name))
        alf_data += symbol.mangled_name
        write_u32(len(symbol.demangled_name))
        alf_data += symbol.demangled_name
        write_u32(symbol.addr)
        write_u32(symbol.size)
        write_u32(1 if symbol.is_data else 0)
        write_u32(symbol.section_id)
        if version == 105:
            write_u32(symbol.unk_10)

    write_u32(0)
    write_u32(0)
    write_u32_to(table_size_offs, len(alf_data) - table_size_offs - 4)

    return alf_data


Path('sample.dol').write_bytes(make_dol())
Path('sample_104.alf').write_bytes(make_alf(104))
Path('sample_105.alf').write_bytes(make_alf(105))
