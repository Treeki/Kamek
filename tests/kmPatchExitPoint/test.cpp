#include <kamek.h>

extern int externalSym;
int localSym;

void localFunc01() {}
void localFunc02() {}
void localFunc03() {}
void localFunc04() {}

kmPatchExitPoint(localFunc01, 0x80000000);
kmPatchExitPoint(localFunc02, &externalSym);
kmPatchExitPoint(localFunc03, &localSym);
kmPatchExitPoint(localFunc04, 0);
