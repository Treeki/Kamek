#include <kamek_asm.S>

.extern externalSym1, externalSym2
.long 0
localSym: .long 0

localFunc:
    b externalSym1
    b localSym
    // beq externalSym1  // not possible
    beq localSym
    li r0, externalSym1@h
    li r0, externalSym2@h
    li r0, localSym@h
    li r0, externalSym1@ha
    li r0, externalSym2@ha
    li r0, localSym@ha
    li r0, externalSym1@l
    li r0, externalSym2@l
    li r0, localSym@l
