#include <kamek.h>

extern int externalSym;
int localSym;

extern int externalFunc01;
extern int externalFunc02;
extern int externalFunc03;

kmBranch(0x80000000, 0x80000004);
kmBranch(0x80000010, &externalSym);
kmBranch(0x80000020, &localSym);
kmBranch(&externalFunc01, 0x80000034);
kmBranch(&externalFunc02, &externalSym);
kmBranch(&externalFunc03, &localSym);
