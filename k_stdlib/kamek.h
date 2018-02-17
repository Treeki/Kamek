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

#define NULL 0

typedef unsigned char u8;
typedef unsigned short u16;
typedef unsigned int u32;
typedef unsigned long long u64;
typedef signed char s8;
typedef signed short s16;
typedef signed int s32;
typedef signed long long s64;
typedef float f32;
typedef double f64;
typedef volatile s64 vs64;
typedef volatile s32 vs32;
typedef volatile s16 vs16;
typedef volatile s8 vs8;
typedef volatile u64 vu64;
typedef volatile u32 vu32;
typedef volatile u16 vu16;
typedef volatile u8 vu8;
typedef volatile f64 vf64;
typedef volatile f32 vf32;

typedef unsigned long size_t;

#include "base/hooks.h"
#include "base/c_stdlib.h"
#include "base/rvl_sdk.h"

#endif

