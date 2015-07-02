#include "k_stdlib/kamek.h"


kmWrite32(0x80123456, 0x99998888);
kmWrite32(0x80123456, 0x99998888);
kmWrite32(0x80123456, 0x99998888);
kmWrite32(0x80123456, 0x99998888);
kmWrite32(0x80123456, 0x99998888);
kmWrite32(0x80123456, 0x99998888);
kmWrite32(0x80123456, 0x99998888);
kmWrite32(0x80123456, 0x99998888);
kmWrite32(0x80123456, 0x99998888);

kmWrite32(0x23232323, "beep");

extern int foo();

int multiplier(int a, int b)
{
	return a * b;
}

int foo_added()
{
	return foo() + foo();
}


int namedFn(int value)
{
	foo();
	return value;
}
kmBranch(0x80B6C488, namedFn);

kmBranchDefCpp(0x80B6C488, NULL, int, int state)
{
	foo();
	foo();
	foo();
	return state;
}

kmBranchDefAsm(0x80B6C488, 0x80B6C48C)
{
	li r5, 1;
}


extern "C" void baz();
asm void tryMe()
{
	li r3, 1;
	li r4, 2;
	b baz;
}

kmWrite32(0x64646464, foo_added);
kmWrite32(0x90000000, tryMe);

