#include <kamek.h>

extern int externalSym;
int localSym;

kmCondWritePointer(0x80000000, 0x80000004, 0x80000008);
kmCondWritePointer(0x80000010, 0x80000014, &externalSym);
kmCondWritePointer(0x80000020, 0x80000024, &localSym);
kmCondWritePointer(0x80000030, &externalSym, 0x80000038);
kmCondWritePointer(0x80000040, &externalSym, &externalSym);
kmCondWritePointer(0x80000050, &externalSym, &localSym);

kmCondWrite32(0x80000100, 0x11111111, 0x22222222);
kmCondWrite16(0x80000110, 0x3333, 0x4444);
kmCondWrite8(0x80000120, 0x55, 0x66);
kmCondWriteFloat(0x80000130, 7.7f, 8.8f);
kmCondWriteNop(0x80000140, 0x99999999);

kmWritePointer(0x80000200, 0x80000204);
kmWritePointer(0x80000210, &externalSym);
kmWritePointer(0x80000220, &localSym);

kmWrite32(0x80000300, 0x11111111);
kmWrite16(0x80000310, 0x2222);
kmWrite8(0x80000320, 0x33);
kmWriteFloat(0x80000330, 4.4f);
kmWriteNop(0x80000340);
