#include <kamek.h>

extern "C" unsigned int externalSym1();
extern "C" unsigned int externalSym2();

__attribute__ ((noinline)) unsigned int placeholder() { return 0; };
__attribute__ ((noinline)) unsigned int localSym() { return 1; };

unsigned int localFunc() {
    unsigned int value = 0;

    externalSym1();
    externalSym2();
    localSym();

    value ^= (unsigned int)externalSym1;
    value ^= (unsigned int)externalSym2;
    value ^= (unsigned int)localSym;

    return value;
}

asm void localFuncAsm() {
    b externalSym1
    b localSym
    // beq externalSym1  // not possible
    // beq localSym  // intentionally not allowed in C++, since you can't guarantee
                     // that the other function will be close enough to fit in a REL14
    li r0, externalSym1@h
    li r0, externalSym2@h
    li r0, localSym@h
    li r0, externalSym1@ha
    li r0, externalSym2@ha
    li r0, localSym@ha
    li r0, externalSym1@l
    li r0, externalSym2@l
    li r0, localSym@l
}
