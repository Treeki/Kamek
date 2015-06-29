/*
 * Kamek Standard Library
 * Wii game patching engine
 * (c) Treeki 2010-2015
 */

#ifndef __KAMEK_STDLIB_MAIN_H
#define __KAMEK_STDLIB_MAIN_H

#ifndef __MWERKS__
#error "Kamek requires the CodeWarrior compiler!"
#endif

#define NULL 0

typedef unsigned char u8;
typedef unsigned short u16;
typedef unsigned int u32;
typedef signed char s8;
typedef signed short s16;
typedef signed int s32;
typedef float f32;
typedef double f64;

#pragma section ".kamek"

#define kmIdentifier(key, counter) \
	_k##key##_##counter
#define kmCommandInt(type, key, counter) \
	__declspec (section ".kamek") static const type kmIdentifier(Cmd##key, counter)
#define kmCommand(type, key) \
	kmCommandInt(type, key, __COUNTER__)

// kmWrite: Write value to address
#define kmWrite32(addr, value) kmCommand(u32, Write32) \
	[2] = { (u32)(addr), (u32)(value) }
#define kmWrite16(addr, value) kmCommand(u32, Write16) \
	[2] = { (u32)(addr), (u32)(value) }
#define kmWrite8(addr, value) kmCommand(u32, Write8) \
	[2] = { (u32)(addr), (u32)(value) }

// kmPatchExitPoint: Force the end of a Kamek function to
// always jump to a specific address
//   (if the address is 0, the end remains as-is (i.e. blr))
#define kmPatchExitPoint(addr, value) kmCommand(u32, PatchExit) \
	[2] = { (u32)(addr), (u32)(value) }

// kmBranch: Set up a branch from a specific instruction
// to a specific address
#define kmBranch(addr, value) kmCommand(u32, Branch) \
	[2] = { (u32)(addr), (u32)(value) }

// kmMakeBranch: Set up a branch from a specific instruction
// to a function defined with this call

#define kmMakeBranchInt(counter, addr, exitPoint, returnType, ...) \
	returnType kmIdentifier(UserFunc, counter) (__VA_ARGS__); \
	kmBranch(addr, kmIdentifier(UserFunc, counter)); \
	kmPatchExitPoint(kmIdentifier(UserFunc, counter), exitPoint); \
	returnType kmIdentifier(UserFunc, counter) (__VA_ARGS__)

#define kmMakeBranch(addr, exitPoint, returnType, ...) \
	kmMakeBranchInt(__COUNTER__, addr, exitPoint, returnType, __VA_ARGS__)

#define kmMakeAsmBranch(addr, exitPoint) \
	kmMakeBranchInt(__COUNTER__, addr, exitPoint, asm void, )


#endif

