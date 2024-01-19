#ifndef __KAMEK_BASE_HOOKS_H
#define __KAMEK_BASE_HOOKS_H

#include "base/context.h"


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
	__declspec (section ".kamek") static const u32 kmIdentifier(Hook, counter)

// general hook definition macros
// TODO: debugging data (file, line, ...) for diagnostic use by Kamek maybe? :3
#define kmHook0(type) \
	kmHookInt(__COUNTER__)[2] = { 0, (type) }
#define kmHook1(type, arg0) \
	kmHookInt(__COUNTER__)[3] = { 1, (type), (u32)(arg0) }
#define kmHook2(type, arg0, arg1) \
	kmHookInt(__COUNTER__)[4] = { 2, (type), (u32)(arg0), (u32)(arg1) }
#define kmHook3(type, arg0, arg1, arg2) \
	kmHookInt(__COUNTER__)[5] = { 3, (type), (u32)(arg0), (u32)(arg1), (u32)(arg2) }
#define kmHook4(type, arg0, arg1, arg2, arg3) \
	kmHookInt(__COUNTER__)[6] = { 4, (type), (u32)(arg0), (u32)(arg1), (u32)(arg2), (u32)(arg3) }

// kmCondWrite
//   Write value to address, conditionally
#define kmCondWritePointer(addr, original, value) kmHook4(kctConditionalWrite, 1, (addr), (value), (original))
#define kmCondWrite32(addr, original, value) kmHook4(kctConditionalWrite, 2, (addr), (value), (original))
#define kmCondWrite16(addr, original, value) kmHook4(kctConditionalWrite, 3, (addr), (value), (original))
#define kmCondWrite8(addr, original, value) kmHook4(kctConditionalWrite, 4, (addr), (value), (original))

// kmWrite
//   Write value to address
#define kmWritePointer(addr, ptr) kmHook3(kctWrite, 1, (addr), (ptr))
#define kmWrite32(addr, value) kmHook3(kctWrite, 2, (addr), (value))
#define kmWrite16(addr, value) kmHook3(kctWrite, 3, (addr), (value))
#define kmWrite8(addr, value) kmHook3(kctWrite, 4, (addr), (value))

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

// kmSafeBranchDefCpp
//   Set up a branch (b) from a specific instruction to an auto-generated
//   trampoline that saves all registers before calling a function
//   defined directly underneath, and restores them afterward.
#define kmSafeBranchDefInt(counter, addr, ...) \
	static void kmIdentifier(UserFunc, counter) (kmContext &_kctx); \
	static asm void kmIdentifier(Trampoline, counter) () { \
		nofralloc                                       ; \
		kmSaveContext                                   ; \
		bl kmIdentifier(UserFunc, counter)              ; \
		kmRestoreContext                                ; \
		__VA_ARGS__  /* run the original instruction */ ; \
		blr      /* Kamek will replace this with a b */ ; \
	}; \
	kmBranch(addr, kmIdentifier(Trampoline, counter)); \
	kmPatchExitPoint(kmIdentifier(Trampoline, counter), addr + 4); \
	static void kmIdentifier(UserFunc, counter) (kmContext &_kctx)

// The way this is implemented is wonky, but kmSafeBranchDefCpp
// basically supports two forms:
//     kmSafeBranchDefCpp(addr)
//     kmSafeBranchDefCpp(addr, instruction)
// "instruction" is the original instruction you're hooking on, if you'd
// like to execute a copy of it in the trampoline to avoid losing it
// from the function being hooked into.
// Examples:
//     kmSafeBranchDefCpp(0x80345678) { ... }
//     kmSafeBranchDefCpp(0x80345678, addi r3, r4, 5) { ... }
// For an explanation of the macro implementation, see
// https://codecraft.co/2014/11/25/variadic-macros-tricks/
#define _GET_9TH_ARG(_1, _2, _3, _4, _5, _6, _7, _8, N, ...) N
#define kmSafeBranchDefCpp1(addr) \
	kmSafeBranchDefInt(__COUNTER__, addr,)
#define kmSafeBranchDefCpp2(addr, _1) \
	kmSafeBranchDefInt(__COUNTER__, addr, _1)
#define kmSafeBranchDefCpp3(addr, _1, _2) \
	kmSafeBranchDefInt(__COUNTER__, addr, _1, _2)
#define kmSafeBranchDefCpp4(addr, _1, _2, _3) \
	kmSafeBranchDefInt(__COUNTER__, addr, _1, _2, _3)
#define kmSafeBranchDefCpp5(addr, _1, _2, _3, _4) \
	kmSafeBranchDefInt(__COUNTER__, addr, _1, _2, _3, _4)
#define kmSafeBranchDefCpp6(addr, _1, _2, _3, _4, _5) \
	kmSafeBranchDefInt(__COUNTER__, addr, _1, _2, _3, _4, _5)
#define kmSafeBranchDefCpp7(addr, _1, _2, _3, _4, _5, _6) \
	kmSafeBranchDefInt(__COUNTER__, addr, _1, _2, _3, _4, _5, _6)
#define kmSafeBranchDefCpp(...) \
	_GET_9TH_ARG(, ##__VA_ARGS__, \
	kmSafeBranchDefCpp7, kmSafeBranchDefCpp6, kmSafeBranchDefCpp5, \
	kmSafeBranchDefCpp4, kmSafeBranchDefCpp3, kmSafeBranchDefCpp2, \
	kmSafeBranchDefCpp1,)(__VA_ARGS__)

// For use in kmSafeBranchDefCpp functions. Maps a register in the
// hooked context to a local variable of some user-specified
// 32-bit-sized type
#define kmUseReg(reg, name, type) \
	type &name = reinterpret_cast<type &>(_kctx.reg);


#endif
