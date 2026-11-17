#include <kamek.h>

extern int externalFunc;

kmCallDefAsm(0x80000000) { li r3, 1 }
kmCallDefAsm(&externalFunc) { li r3, 2 }
