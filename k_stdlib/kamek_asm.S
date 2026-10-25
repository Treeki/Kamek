kctWrite .equ 1
kctConditionalWrite .equ 2
kctInjectBranch .equ 3
kctInjectCall .equ 4
kctPatchExit .equ 5

KM_INJECT_STRIP_BLR_PAST .equ 0x1
KM_INJECT_ADD_PADDING .equ 0x2


// general hook definition macros
kmHook0: .macro type
	.section .kamek
	_kHook\@: .long 0, type
	.previous
	.section .discard
	li r0, (_kHook\@)@l  // reference to prevent CodeWarrior from deleting the symbol
	.previous
	.endm
kmHook1: .macro type, arg0
	.section .kamek
	_kHook\@: .long 1, type, arg0
	.previous
	.section .discard
	li r0, (_kHook\@)@l  // reference to prevent CodeWarrior from deleting the symbol
	.previous
	.endm
kmHook2: .macro type, arg0, arg1
	.section .kamek
	_kHook\@: .long 2, type, arg0, arg1
	.previous
	.section .discard
	li r0, (_kHook\@)@l  // reference to prevent CodeWarrior from deleting the symbol
	.previous
	.endm
kmHook3: .macro type, arg0, arg1, arg2
	.section .kamek
	_kHook\@: .long 3, type, arg0, arg1, arg2
	.previous
	.section .discard
	li r0, (_kHook\@)@l  // reference to prevent CodeWarrior from deleting the symbol
	.previous
	.endm
kmHook4: .macro type, arg0, arg1, arg2, arg3
	.section .kamek
	_kHook\@: .long 4, type, arg0, arg1, arg2, arg3
	.previous
	.section .discard
	li r0, (_kHook\@)@l  // reference to prevent CodeWarrior from deleting the symbol
	.previous
	.endm

kmHook_2ui_1f: .macro type, arg0, arg1, arg2
	.section .kamek
	_kHook\@: .long 3, type, arg0, arg1
	.float arg2
	.previous
	.section .discard
	li r0, (_kHook\@)@l  // reference to prevent CodeWarrior from deleting the symbol
	.previous
	.endm
kmHook_2ui_2f: .macro type, arg0, arg1, arg2, arg3
	.section .kamek
	_kHook\@: .long 4, type, arg0, arg1
	.float arg2, arg3
	.previous
	.section .discard
	li r0, (_kHook\@)@l  // reference to prevent CodeWarrior from deleting the symbol
	.previous
	.endm

// kmCondWrite
//   Write value to address, conditionally
kmCondWritePointer: .macro addr, original, ptr
	kmHook4 kctConditionalWrite, 1, addr, ptr, original
	.endm
kmCondWrite32: .macro addr, original, value
	kmHook4 kctConditionalWrite, 2, addr, value, original
	.endm
kmCondWrite16: .macro addr, original, value
	kmHook4 kctConditionalWrite, 3, addr, value, original
	.endm
kmCondWrite8: .macro addr, original, value
	kmHook4 kctConditionalWrite, 4, addr, value, original
	.endm
kmCondWriteFloat: .macro addr, original, value
	kmHook_2ui_2f kctConditionalWrite, 2, addr, value, original
	.endm
kmCondWriteNop: .macro addr, original
	kmCondWrite32 addr, original, 0x60000000
	.endm

// kmWrite
//   Write value to address
kmWritePointer: .macro addr, ptr
	kmHook3 kctWrite, 1, addr, ptr
	.endm
kmWrite32: .macro addr, value
	kmHook3 kctWrite, 2, addr, value
	.endm
kmWrite16: .macro addr, value
	kmHook3 kctWrite, 3, addr, value
	.endm
kmWrite8: .macro addr, value
	kmHook3 kctWrite, 4, addr, value
	.endm
kmWriteFloat: .macro addr, value
	kmHook_2ui_1f kctWrite, 2, addr, value
	.endm

// kmBranch, kmCall
//   Set up a branch from a specific instruction to a specific address
kmBranch: .macro addr, ptr
	kmHook2 kctInjectBranch, addr, ptr
	.endm
kmCall: .macro addr, ptr
	kmHook2 kctInjectCall, addr, ptr
	.endm

// kmBranchDef
//   Set up a branch (b) from a specific instruction to a function defined
//   directly underneath.
kmBranchDef: .macro addr
	kmBranch addr, __kUserFuncBranch\@
	__kUserFuncBranch\@:
	.endm

// kmCallDef
//   Set up a call (bl) from a specific instruction to a function defined
//   directly underneath.
kmCallDef: .macro addr
	kmCall addr, __kUserFuncCall\@
	__kUserFuncCall\@:
	.endm

// kmWriteDefStart startAddr[, endAddr[, flags[, pad]]]
// kmWriteDefEnd
//   Inject assembly code directly into the target executable, overwriting
//   whatever's there. startAddr and endAddr are the addresses of the first and
//   last instructions to replace.
//   - If endAddr is omitted, it defaults to startAddr.
//   - flags specifies options. Default is `KM_INJECT_STRIP_BLR_PAST | KM_INJECT_ADD_PADDING`.
//   - pad is the value to fill extra space with if KM_INJECT_ADD_PADDING is set.
//     Default is nop (0x60000000).
kmWriteDefStart: .macro startAddr, endAddr, flags, pad
	.section .km_inject_\@_meta
	_kInjectionMeta\@: .long 4, startAddr
	.if narg >= 2
		.long endAddr
	.else
		.long startAddr
	.endif
	.if narg >= 3
		.long flags
	.else
		.long KM_INJECT_STRIP_BLR_PAST | KM_INJECT_ADD_PADDING
	.endif
	.if narg >= 4
		.long pad
	.else
		.long 0x60000000
	.endif
	.previous
	.section .discard
	li r0, (_kInjectionMeta\@)@l    // reference to prevent CodeWarrior from deleting the symbol
	li r0, (__kUserFuncInject\@)@l  // ditto
	.previous
	.section .km_inject_\@
	__kUserFuncInject\@:
	.endm
kmWriteDefEnd: .macro
	.previous
	.endm

// kmWriteNop, kmWriteNops
//   Write one or more nop instructions to an address or address range
kmWriteNop: .macro addr
	kmWrite32 addr, 0x60000000
	.endm
kmWriteNops: .macro startAddr, endAddr
	kmWriteDefStart startAddr, endAddr
	nop
	kmWriteDefEnd
	.endm

// kamek_b, kamek_bl
//   Branch to or call a direct code address from the original game executable.
//   This allows Kamek's address mapping functionality to be used without
//   having to add single-use externals.
kamek_b: .macro addr
	.extern __kAutoMap_&&addr
	b __kAutoMap_&&addr
	.endm
kamek_bl: .macro addr
	.extern __kAutoMap_&&addr
	bl __kAutoMap_&&addr
	.endm

// kamek_ref_address
//   Load a direct data address from the original game executable.
//   This allows Kamek's address mapping functionality to be used without
//   having to add single-use externals.
// Example:
//   Set r3 to this address (mapped):
//     kamek_ref_address r3, 0x8098DC28
kamek_ref_address: .macro output_reg, addr
	.extern __kAutoMap_&&addr
	lis output_reg, __kAutoMap_&&addr@ha
	addi output_reg, output_reg, __kAutoMap_&&addr@l
	.endm

// kamek_ref
//   Reference something at a direct address from the original executable.
//   This requires you to supply a temporary register which you know is
//    safe to overwrite.
//   When loading an integer, you can supply the same output_reg and temp_reg.
//
// Examples:
//   Load the 32-bit value at this address into r3:
//     kamek_ref lwz, r3, 0x8042A4A8, r3
//   Store r7 as a byte at this address, using r10 as a temporary:
//     kamek_ref stb, r7, 0x80B07DD0, r10
//   Load or store the 32-bit float at this address from/into f1, using r10 as a temporary:
//     kamek_ref lfs, f1, 0x80AD3400, r10
//     kamek_ref stf, f1, 0x80AD3400, r10
kamek_ref: .macro insn, output_reg, addr, temp_reg
	.extern __kAutoMap_&&addr
	lis temp_reg, __kAutoMap_&&addr@ha
	insn output_reg, __kAutoMap_&&addr@l(temp_reg)
	.endm

// kamek_const_float
//   Load a constant float.
//   This requires a temporary non-float register.
kamek_const_float: .macro output_reg, float_value, temp_reg
	.data
__kConstFloat\@: .float float_value
	.previous
	lis temp_reg, (__kConstFloat\@)@ha
	lfs output_reg, (__kConstFloat\@)@l(temp_reg)
	.endm


// Default to .text so that Kamek macros work
.text

