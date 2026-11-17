#include <kamek.h>

extern int externalSym;
int localSym;

extern int externalFunc01;
extern int externalFunc02;
extern int externalFunc03;
extern int externalFunc04;

kmBranchDefAsm(0x80000000, 0x80000004) { li r3, 1 }
kmBranchDefAsm(0x80000010, &externalSym) { li r3, 2 }
kmBranchDefAsm(0x80000020, &localSym) { li r3, 3 }
kmBranchDefAsm(0x80000030, 0) { li r3, 4 }
kmBranchDefAsm(&externalFunc01, 0x80000044) { li r3, 5 }
kmBranchDefAsm(&externalFunc02, &externalSym) { li r3, 6 }
kmBranchDefAsm(&externalFunc03, &localSym) { li r3, 7 }
kmBranchDefAsm(&externalFunc04, 0) { li r3, 8 }
