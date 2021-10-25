#ifndef __KAMEK_BASE_HOOKS_H
#define __KAMEK_BASE_HOOKS_H

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

#endif
