#include <kamek_asm.S>

.extern externalFunc

kmBranchDef 0x80000000
    li r3, 1
    blr
kmBranchDef externalFunc
    li r3, 2
    blr
