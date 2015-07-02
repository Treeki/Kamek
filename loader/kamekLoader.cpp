#include "kamekLoader.h"

// How to deal with crap, apparently:
// __dcbst(ptr, offset)
// __sync()
// nothing for icbi???
//
// And at the end:
// __sync()
// __isync()



void loadKamekBinary(const loaderFunctions *funcs, const char *path)
{
	funcs->OSReport("{Kamek by Treeki}\nLoading Kamek binary '%s'...\n", path);

	register const char *beep = path;
	asm {
		dcbst r0, beep;
		sync;
		icbi r0, beep;
	}
}

