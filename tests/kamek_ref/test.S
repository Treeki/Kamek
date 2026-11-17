#include <kamek_asm.S>

func:
    kamek_ref lwz, r3, 0x81000000, r3
    kamek_ref lwz, r3, 0x81000004, r4
    kamek_ref stb, r5, 0x81000008, r5
    kamek_ref stb, r5, 0x8100000c, r6
    kamek_ref lfs, f1, 0x81000010, r3
    kamek_ref stfs, f1, 0x81000014, r3
