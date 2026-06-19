#include <kamek.h>

extern int externalSym;
int localSym;

extern int externalFunc01;
extern int externalFunc02;
extern int externalFunc03;
extern int externalFunc04;

kmBranchDefCpp(0x80000000, 0x80000004, int, ) { return 1; }
kmBranchDefCpp(0x80000010, &externalSym, int, ) { return 2; }
kmBranchDefCpp(0x80000020, &localSym, int, ) { return 3; }
kmBranchDefCpp(0x80000030, 0, int, ) { return 4; }
kmBranchDefCpp(&externalFunc01, 0x80000044, int, ) { return 5; }
kmBranchDefCpp(&externalFunc02, &externalSym, int, ) { return 6; }
kmBranchDefCpp(&externalFunc03, &localSym, int, ) { return 7; }
kmBranchDefCpp(&externalFunc04, 0, int, ) { return 8; }
