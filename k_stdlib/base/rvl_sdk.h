#ifndef __KAMEK_BASE_RVL_SDK_H
#define __KAMEK_BASE_RVL_SDK_H

#ifdef __cplusplus
extern "C" {
#endif

/* OS Module */
void OSReport(const char *format, ...);
u64 OSGetTime();
u32 OSGetTick();
void OSFatal(u32 *fg, u32 *bg, const char *message);

typedef struct {
	int sec, min, hour, mday, mon, year, wday, yday, msec, usec;
} OSCalendarTime;
void OSTicksToCalendarTime(u64 time, OSCalendarTime *result);

/* MTX Module */
typedef struct { f32 x, y; } Vec2;
typedef struct { f32 x, y, z; } Vec;
typedef struct { s16 x, y, z; } S16Vec;
typedef f32 Mtx[3][4];
typedef f32 Mtx44[4][4];
typedef struct { f32 x, y, z, w; } Quaternion;

void PSMTXIdentity(Mtx matrix);
void PSMTXCopy(const Mtx source, Mtx dest);
void PSMTXConcat(const Mtx sourceA, const Mtx sourceB, Mtx dest);
void PSMTXConcatArray(const Mtx sourceA, const Mtx *sourcesB, Mtx *destsB, int count);
u32 PSMTXInverse(const Mtx source, Mtx dest);
u32 PSMTXInvXpose(const Mtx source, Mtx dest);
void PSMTXRotRad(Mtx matrix, u8 axis, f32 radians);
void PSMTXRotTrig(Mtx matrix, u8 axis, f32 sin, f32 cos);
void PSMTXRotAxisRad(Mtx matrix, Vec *axis, f32 radians);
void PSMTXTrans(Mtx matrix, f32 x, f32 y, f32 z);
void PSMTXTransApply(const Mtx source, Mtx dest, f32 x, f32 y, f32 z);
void PSMTXScale(Mtx matrix, f32 x, f32 y, f32 z);
void PSMTXScaleApply(const Mtx source, Mtx dest, f32 x, f32 y, f32 z);
void PSMTXQuat(Mtx dest, const Quaternion *quat);
void C_MTXLookAt(Mtx dest, const Vec *cameraPos, const Vec *cameraUp, const Vec *target);
void C_MTXLightFrustum(Mtx dest, f32 top, f32 bottom, f32 left, f32 right, f32 near, f32 scaleS, f32 scaleT, f32 transS, f32 transT);
void C_MTXLightPerspective(Mtx dest, f32 fovy, f32 aspect, f32 scaleS, f32 scaleT, f32 transS, f32 transT);
void C_MTXLightOrtho(Mtx dest, f32 top, f32 bottom, f32 left, f32 right, f32 scaleS, f32 scaleT, f32 transS, f32 transT);
void PSMTXMultVec(const Mtx matrix, const Vec *source, Vec *dest);
void C_MTXFrustum(Mtx44 dest, f32 top, f32 bottom, f32 left, f32 right, f32 near, f32 far);
void C_MTXPerspective(Mtx44 dest, f32 fovy, f32 aspect, f32 near, f32 far);
void C_MTXOrtho(Mtx44 dest, f32 top, f32 bottom, f32 left, f32 right, f32 near, f32 far);
void PSVECAdd(const Vec *sourceA, const Vec *sourceB, Vec *dest);
void PSVECSubtract(const Vec *sourceA, const Vec *sourceB, Vec *dest);
void PSVECScale(const Vec *source, Vec *dest, f32 scale);
void PSVECNormalize(const Vec *source, Vec *dest);
f32 PSVECMag(const Vec *vec);
f32 PSVECDotProduct(const Vec *sourceA, const Vec *sourceB);
void PSVECCrossProduct(const Vec *sourceA, const Vec *sourceB, Vec *dest);
void C_VECHalfAngle(const Vec *sourceA, const Vec *sourceB, Vec *dest);
f32 PSVECSquareDistance(const Vec *sourceA, const Vec *sourceB);
void C_QUATMtx(Quaternion *dest, const Mtx source);
void C_QUATSlerp(const Quaternion *sourceA, const Quaternion *sourceB, Quaternion *dest, f32 value);

#define MTXIdentity PSMTXIdentity
#define MTXCopy PSMTXCopy
#define MTXConcat PSMTXConcat
#define MTXConcatArray PSMTXConcatArray
#define MTXInverse PSMTXInverse
#define MTXInvXpose PSMTXInvXpose
#define MTXRotRad PSMTXRotRad
#define MTXRotTrig PSMTXRotTrig
#define MTXRotAxisRad PSMTXRotAxisRad
#define MTXTrans PSMTXTrans
#define MTXTransApply PSMTXTransApply
#define MTXScale PSMTXScale
#define MTXScaleApply PSMTXScaleApply
#define MTXQuat PSMTXQuat
#define MTXLookAt C_MTXLookAt
#define MTXLightFrustum C_MTXLightFrustum
#define MTXLightPerspective C_MTXLightPerspective
#define MTXLightOrtho C_MTXLightOrtho
#define MTXMultVec PSMTXMultVec
#define MTXFrustum C_MTXFrustum
#define MTXPerspective C_MTXPerspective
#define MTXOrtho C_MTXOrtho
#define VECAdd PSVECAdd
#define VECSubtract PSVECSubtract
#define VECScale PSVECScale
#define VECNormalize PSVECNormalize
#define VECMag PSVECMag
#define VECDotProduct PSVECDotProduct
#define VECCrossProduct PSVECCrossProduct
#define VECHalfAngle C_VECHalfAngle
#define VECSquareDistance PSVECSquareDistance
#define QUATMtx C_QUATMtx
#define QUATSlerp C_QUATSlerp

// TODO: GX, CX, IOS ... and then of course NW4R

#ifdef __cplusplus
}
#endif

#endif
