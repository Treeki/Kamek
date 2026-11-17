#include <kamek_asm.S>

.extern externalFunc

kmCallDef 0x80000000
    li r3, 1
    blr
kmCallDef externalFunc
    li r3, 2
    blr
