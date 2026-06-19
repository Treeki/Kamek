#include <kamek.h>

extern int externalFunc;

kmCallDefCpp(0x80000000, int, ) { return 1; }
kmCallDefCpp(&externalFunc, int, ) { return 2; }
