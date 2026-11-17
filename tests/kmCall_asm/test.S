#include <kamek_asm.S>

.extern externalSym
localSym: .long 0

.extern externalFunc01
.extern externalFunc02
.extern externalFunc03

kmCall 0x80000000, 0x80000004
kmCall 0x80000010, externalSym
kmCall 0x80000020, localSym
kmCall externalFunc01, 0x80000034
kmCall externalFunc02, externalSym
kmCall externalFunc03, localSym
