/*
 * Kamek Standard Library
 * Wii game patching engine
 * (c) Treeki 2010-2018
 */

#ifndef __KAMEK_H
#define __KAMEK_H

#ifndef __MWERKS__
#error "Kamek requires the CodeWarrior compiler!"
#endif

// allow Kamek hooks to be defined from C++ source files
#pragma section ".kamek"

// hook type IDs _must_ match what's in the Kamek source!
#define kctWrite 1
#define kctConditionalWrite 2
#define kctInjectBranch 3
#define kctInjectCall 4
#define kctPatchExit 5


#define kmIdentifier(key, counter) \
	_k##key##counter
#define kmHookInt(counter) \
	__declspec (section ".kamek") static const unsigned int kmIdentifier(Hook, counter)

struct _kmHook_4ui_1f_t { unsigned int a; unsigned int b; unsigned int c; unsigned int d; float e; };
#define _kmHook_4ui_1f(counter) \
	__declspec (section ".kamek") static const _kmHook_4ui_1f_t kmIdentifier(Hook, counter)
struct _kmHook_4ui_2f_t { unsigned int a; unsigned int b; unsigned int c; unsigned int d; float e; float f; };
#define _kmHook_4ui_2f(counter) \
	__declspec (section ".kamek") static const _kmHook_4ui_2f_t kmIdentifier(Hook, counter)

// general hook definition macros
// TODO: debugging data (file, line, ...) for diagnostic use by Kamek maybe? :3
#define kmHook0(type) \
	kmHookInt(__COUNTER__)[2] = { 0, (type) }
#define kmHook1(type, arg0) \
	kmHookInt(__COUNTER__)[3] = { 1, (type), (unsigned int)(arg0) }
#define kmHook2(type, arg0, arg1) \
	kmHookInt(__COUNTER__)[4] = { 2, (type), (unsigned int)(arg0), (unsigned int)(arg1) }
#define kmHook3(type, arg0, arg1, arg2) \
	kmHookInt(__COUNTER__)[5] = { 3, (type), (unsigned int)(arg0), (unsigned int)(arg1), (unsigned int)(arg2) }
#define kmHook4(type, arg0, arg1, arg2, arg3) \
	kmHookInt(__COUNTER__)[6] = { 4, (type), (unsigned int)(arg0), (unsigned int)(arg1), (unsigned int)(arg2), (unsigned int)(arg3) }

#define kmHook_2ui_1f(type, arg0, arg1, arg2) \
	_kmHook_4ui_1f(__COUNTER__) = { 3, (type), (unsigned int)(arg0), (unsigned int)(arg1), (float)(arg2) }
#define kmHook_2ui_2f(type, arg0, arg1, arg2, arg3) \
	_kmHook_4ui_2f(__COUNTER__) = { 4, (type), (unsigned int)(arg0), (unsigned int)(arg1), (float)(arg2), (float)(arg3) }

// kmCondWrite
//   Write value to address, conditionally
#define kmCondWritePointer(addr, original, value) kmHook4(kctConditionalWrite, 1, (addr), (value), (original))
#define kmCondWrite32(addr, original, value) kmHook4(kctConditionalWrite, 2, (addr), (value), (original))
#define kmCondWrite16(addr, original, value) kmHook4(kctConditionalWrite, 3, (addr), (value), (original))
#define kmCondWrite8(addr, original, value) kmHook4(kctConditionalWrite, 4, (addr), (value), (original))
#define kmCondWriteFloat(addr, original, value) kmHook_2ui_2f(kctConditionalWrite, 2, (addr), (value), (original))

// kmWrite
//   Write value to address
#define kmWritePointer(addr, ptr) kmHook3(kctWrite, 1, (addr), (ptr))
#define kmWrite32(addr, value) kmHook3(kctWrite, 2, (addr), (value))
#define kmWrite16(addr, value) kmHook3(kctWrite, 3, (addr), (value))
#define kmWrite8(addr, value) kmHook3(kctWrite, 4, (addr), (value))
#define kmWriteFloat(addr, value) kmHook_2ui_1f(kctWrite, 2, (addr), (value))

// kmPatchExitPoint
//   Force the end of a Kamek function to always jump to a specific address
//   (if the address is 0, the end remains as-is (i.e. blr))
#define kmPatchExitPoint(funcStart, dest) kmHook2(kctPatchExit, (funcStart), (dest))

// kmBranch, kmCall
//   Set up a branch from a specific instruction to a specific address
#define kmBranch(addr, ptr) kmHook2(kctInjectBranch, (addr), (ptr))
#define kmCall(addr, ptr) kmHook2(kctInjectCall, (addr), (ptr))

// kmBranchDefCpp, kmBranchDefAsm
//   Set up a branch (b) from a specific instruction to a function defined
//   directly underneath. If exitPoint is not NULL, the function will
//   branch to exitPoint when done; otherwise, it executes blr as normal
#define kmBranchDefInt(counter, addr, exitPoint, returnType, ...) \
	static returnType kmIdentifier(UserFunc, counter) (__VA_ARGS__); \
	kmBranch(addr, kmIdentifier(UserFunc, counter)); \
	kmPatchExitPoint(kmIdentifier(UserFunc, counter), exitPoint); \
	static returnType kmIdentifier(UserFunc, counter) (__VA_ARGS__)

#define kmBranchDefCpp(addr, exitPoint, returnType, ...) \
	kmBranchDefInt(__COUNTER__, addr, exitPoint, returnType, __VA_ARGS__)
#define kmBranchDefAsm(addr, exitPoint) \
	kmBranchDefInt(__COUNTER__, addr, exitPoint, asm void, )

// kmCallDefCpp, kmCallDefAsm
//   Set up a branch with link (bl) from a specific instruction to a function
//   defined directly underneath.
#define kmCallDefInt(counter, addr, returnType, ...) \
	static returnType kmIdentifier(UserFunc, counter) (__VA_ARGS__); \
	kmCall(addr, kmIdentifier(UserFunc, counter)); \
	static returnType kmIdentifier(UserFunc, counter) (__VA_ARGS__)

#define kmCallDefCpp(addr, returnType, ...) \
	kmCallDefInt(__COUNTER__, addr, returnType, __VA_ARGS__)
#define kmCallDefAsm(addr) \
	kmCallDefInt(__COUNTER__, addr, asm void, )

#endif
