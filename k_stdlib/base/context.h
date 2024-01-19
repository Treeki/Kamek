#ifndef __KAMEK_BASE_CONTEXT_H
#define __KAMEK_BASE_CONTEXT_H


typedef struct kmContext {
    /* 0x000 */ unsigned int r0;
    /* 0x004 */ unsigned int r1;  // aka sp
    /* 0x008 */ unsigned int r2;
    /* 0x00C */ unsigned int r3;
    /* 0x010 */ unsigned int r4;
    /* 0x014 */ unsigned int r5;
    /* 0x018 */ unsigned int r6;
    /* 0x01C */ unsigned int r7;
    /* 0x020 */ unsigned int r8;
    /* 0x024 */ unsigned int r9;
    /* 0x028 */ unsigned int r10;
    /* 0x02C */ unsigned int r11;
    /* 0x030 */ unsigned int r12;
    /* 0x034 */ unsigned int r13;
    /* 0x038 */ unsigned int r14;
    /* 0x03C */ unsigned int r15;
    /* 0x040 */ unsigned int r16;
    /* 0x044 */ unsigned int r17;
    /* 0x048 */ unsigned int r18;
    /* 0x04C */ unsigned int r19;
    /* 0x050 */ unsigned int r20;
    /* 0x054 */ unsigned int r21;
    /* 0x058 */ unsigned int r22;
    /* 0x05C */ unsigned int r23;
    /* 0x060 */ unsigned int r24;
    /* 0x064 */ unsigned int r25;
    /* 0x068 */ unsigned int r26;
    /* 0x06C */ unsigned int r27;
    /* 0x070 */ unsigned int r28;
    /* 0x074 */ unsigned int r29;
    /* 0x078 */ unsigned int r30;
    /* 0x07C */ unsigned int r31;
    /* 0x080 */ float f0;
    /* 0x084 */ float f1;
    /* 0x088 */ float f2;
    /* 0x08C */ float f3;
    /* 0x090 */ float f4;
    /* 0x094 */ float f5;
    /* 0x098 */ float f6;
    /* 0x09C */ float f7;
    /* 0x0A0 */ float f8;
    /* 0x0A4 */ float f9;
    /* 0x0A8 */ float f10;
    /* 0x0AC */ float f11;
    /* 0x0B0 */ float f12;
    /* 0x0B4 */ float f13;
    /* 0x0B8 */ float f14;
    /* 0x0BC */ float f15;
    /* 0x0C0 */ float f16;
    /* 0x0C4 */ float f17;
    /* 0x0C8 */ float f18;
    /* 0x0CC */ float f19;
    /* 0x0D0 */ float f20;
    /* 0x0D4 */ float f21;
    /* 0x0D8 */ float f22;
    /* 0x0DC */ float f23;
    /* 0x0E0 */ float f24;
    /* 0x0E4 */ float f25;
    /* 0x0E8 */ float f26;
    /* 0x0EC */ float f27;
    /* 0x0F0 */ float f28;
    /* 0x0F4 */ float f29;
    /* 0x0F8 */ float f30;
    /* 0x0FC */ float f31;
    /* 0x100 */ unsigned int cr;
    /* 0x104 */ unsigned int ctr;
    /* 0x108 */ unsigned int lr;
} kmContext;

// sizeof() and offsetof() don't exist in asm, so these are here for the
// benefit of the kmSaveContext and kmRestoreContext asm macros.
#define _KMCONTEXT_SIZE 0x10C
#define _KMCONTEXT_OFFSET_OF_R0 0x000
#define _KMCONTEXT_OFFSET_OF_SP 0x004
#define _KMCONTEXT_OFFSET_OF_R3 0x00C
#define _KMCONTEXT_OFFSET_OF_LR 0x108


// Store all registers except {r1, r3, lr} to the kmContext struct at r3.
__attribute__((weak)) asm void _kmSaveMostContext() {
    nofralloc
    stw   r0, 0x000(r3)
    // r1 (sp) intentionally skipped: it's likely that the caller has
    // already adjusted sp to create space for the kmContext struct, so
    // it's better for them to fill this in with the desired value
    // themselves
    stw   r2, 0x008(r3)
    // r3 intentionally skipped: it points to the kmContext, not
    // whatever r3's original value was. The caller should write r3
    // before calling this function
    stw   r4, 0x010(r3)
    stw   r5, 0x014(r3)
    stw   r6, 0x018(r3)
    stw   r7, 0x01C(r3)
    stw   r8, 0x020(r3)
    stw   r9, 0x024(r3)
    stw   r10, 0x028(r3)
    stw   r11, 0x02C(r3)
    stw   r12, 0x030(r3)
    stw   r13, 0x034(r3)
    stw   r14, 0x038(r3)
    stw   r15, 0x03C(r3)
    stw   r16, 0x040(r3)
    stw   r17, 0x044(r3)
    stw   r18, 0x048(r3)
    stw   r19, 0x04C(r3)
    stw   r20, 0x050(r3)
    stw   r21, 0x054(r3)
    stw   r22, 0x058(r3)
    stw   r23, 0x05C(r3)
    stw   r24, 0x060(r3)
    stw   r25, 0x064(r3)
    stw   r26, 0x068(r3)
    stw   r27, 0x06C(r3)
    stw   r28, 0x070(r3)
    stw   r29, 0x074(r3)
    stw   r30, 0x078(r3)
    stw   r31, 0x07C(r3)
    stfd  f0, 0x080(r3)
    stfd  f1, 0x084(r3)
    stfd  f2, 0x088(r3)
    stfd  f3, 0x08C(r3)
    stfd  f4, 0x090(r3)
    stfd  f5, 0x094(r3)
    stfd  f6, 0x098(r3)
    stfd  f7, 0x09C(r3)
    stfd  f8, 0x0A0(r3)
    stfd  f9, 0x0A4(r3)
    stfd  f10, 0x0A8(r3)
    stfd  f11, 0x0AC(r3)
    stfd  f12, 0x0B0(r3)
    stfd  f13, 0x0B4(r3)
    stfd  f14, 0x0B8(r3)
    stfd  f15, 0x0BC(r3)
    stfd  f16, 0x0C0(r3)
    stfd  f17, 0x0C4(r3)
    stfd  f18, 0x0C8(r3)
    stfd  f19, 0x0CC(r3)
    stfd  f20, 0x0D0(r3)
    stfd  f21, 0x0D4(r3)
    stfd  f22, 0x0D8(r3)
    stfd  f23, 0x0DC(r3)
    stfd  f24, 0x0E0(r3)
    stfd  f25, 0x0E4(r3)
    stfd  f26, 0x0E8(r3)
    stfd  f27, 0x0EC(r3)
    stfd  f28, 0x0F0(r3)
    stfd  f29, 0x0F4(r3)
    stfd  f30, 0x0F8(r3)
    stfd  f31, 0x0FC(r3)
    mfcr  r0
    stw   r0, 0x100(r3)
    mfctr r0
    stw   r0, 0x104(r3)
    // lr intentionally skipped: bl'ing to this function has clobbered
    // it. The caller should write their desired value
    // (Restore r0 because we clobbered it while saving the SPRs)
    lwz   r0, 0x000(r3)
    blr
}


// Restore all registers except {r1, lr} from the kmContext struct at r3.
__attribute__((weak)) asm void _kmRestoreMostContext() {
    // Do the SPRs first so we can use r0 as a temporary
    lwz   r0, 0x100(r3)
    mtcr  r0
    lwz   r0, 0x104(r3)
    mtctr r0
    // lr intentionally skipped: we need to preserve its current value
    // so we can return to the correct caller. The caller should restore
    // the value themselves at the appropriate time
    lwz   r0, 0x000(r3)
    // r1 (sp) intentionally skipped: would rather not break the ABI
    // invariant that calling a function doesn't change the value of sp.
    // The caller can restore sp themselves when they're ready to
    lwz   r2, 0x008(r3)
    // r3 will be the final register we restore -- skip for now
    lwz   r4, 0x010(r3)
    lwz   r5, 0x014(r3)
    lwz   r6, 0x018(r3)
    lwz   r7, 0x01C(r3)
    lwz   r8, 0x020(r3)
    lwz   r9, 0x024(r3)
    lwz   r10, 0x028(r3)
    lwz   r11, 0x02C(r3)
    lwz   r12, 0x030(r3)
    lwz   r13, 0x034(r3)
    lwz   r14, 0x038(r3)
    lwz   r15, 0x03C(r3)
    lwz   r16, 0x040(r3)
    lwz   r17, 0x044(r3)
    lwz   r18, 0x048(r3)
    lwz   r19, 0x04C(r3)
    lwz   r20, 0x050(r3)
    lwz   r21, 0x054(r3)
    lwz   r22, 0x058(r3)
    lwz   r23, 0x05C(r3)
    lwz   r24, 0x060(r3)
    lwz   r25, 0x064(r3)
    lwz   r26, 0x068(r3)
    lwz   r27, 0x06C(r3)
    lwz   r28, 0x070(r3)
    lwz   r29, 0x074(r3)
    lwz   r30, 0x078(r3)
    lwz   r31, 0x07C(r3)
    lfd   f0, 0x080(r3)
    lfd   f1, 0x084(r3)
    lfd   f2, 0x088(r3)
    lfd   f3, 0x08C(r3)
    lfd   f4, 0x090(r3)
    lfd   f5, 0x094(r3)
    lfd   f6, 0x098(r3)
    lfd   f7, 0x09C(r3)
    lfd   f8, 0x0A0(r3)
    lfd   f9, 0x0A4(r3)
    lfd   f10, 0x0A8(r3)
    lfd   f11, 0x0AC(r3)
    lfd   f12, 0x0B0(r3)
    lfd   f13, 0x0B4(r3)
    lfd   f14, 0x0B8(r3)
    lfd   f15, 0x0BC(r3)
    lfd   f16, 0x0C0(r3)
    lfd   f17, 0x0C4(r3)
    lfd   f18, 0x0C8(r3)
    lfd   f19, 0x0CC(r3)
    lfd   f20, 0x0D0(r3)
    lfd   f21, 0x0D4(r3)
    lfd   f22, 0x0D8(r3)
    lfd   f23, 0x0DC(r3)
    lfd   f24, 0x0E0(r3)
    lfd   f25, 0x0E4(r3)
    lfd   f26, 0x0E8(r3)
    lfd   f27, 0x0EC(r3)
    lfd   f28, 0x0F0(r3)
    lfd   f29, 0x0F4(r3)
    lfd   f30, 0x0F8(r3)
    lfd   f31, 0x0FC(r3)
    // And finally, restore r3, which overwrites our kmContext struct
    // ptr, preventing us from restoring anything else afterwards
    lwz   r3, 0x00C(r3)
    blr
}


// Save the current context to a new stack frame. Leaves a pointer to
// the kmContext struct in r3.
#define kmSaveContext \
    stwu  sp,  -(_KMCONTEXT_SIZE + 8)(sp)        /* allocate */                        ; \
    stw   r3,  (8 + _KMCONTEXT_OFFSET_OF_R3)(sp) /* save r3 */                         ; \
    addi  r3,  sp, (_KMCONTEXT_SIZE + 8)         /* save sp (1/2) */                   ; \
    stw   r3,  (8 + _KMCONTEXT_OFFSET_OF_SP)(sp) /* save sp (2/2) */                   ; \
    mflr  r3                                     /* save lr (1/2) */                   ; \
    stw   r3,  (8 + _KMCONTEXT_OFFSET_OF_LR)(sp) /* save lr (2/2) */                   ; \
    addi  r3,  sp, 8                             /* arg: ptr to context struct */      ; \
    bl _kmSaveMostContext                        /* save the other registers */        ; \
    addi  r3,  sp, 8                             /* leave a ptr to the struct in r3 */ ;


// Restore context previously saved with kmSaveContext.
#define kmRestoreContext \
    addi  r3,  sp, 8                             /* arg: ptr to context struct */ ; \
    bl _kmRestoreMostContext                     /* restore most registers */     ; \
    lwz   r0,  (8 + _KMCONTEXT_OFFSET_OF_LR)(sp) /* restore lr (1/2) */           ; \
    mtlr  r0                                     /* restore lr (2/2) */           ; \
    lwz   r0,  (8 + _KMCONTEXT_OFFSET_OF_R0)(sp) /* restore r0 */                 ; \
    addi  sp, sp, (_KMCONTEXT_SIZE + 8)          /* restore sp */                 ;


#endif
