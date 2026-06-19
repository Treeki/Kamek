#include <kamek.h>

extern int externalSym;
int localSym;

extern int externalFunc01;
extern int externalFunc02;
extern int externalFunc03;

kmCall(0x80000000, 0x80000004);
kmCall(0x80000010, &externalSym);
kmCall(0x80000020, &localSym);
kmCall(&externalFunc01, 0x80000034);
kmCall(&externalFunc02, &externalSym);
kmCall(&externalFunc03, &localSym);
