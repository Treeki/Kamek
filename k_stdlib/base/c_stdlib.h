#ifndef __KAMEK_BASE_C_STDLIB_H
#define __KAMEK_BASE_C_STDLIB_H

#ifdef __cplusplus
extern "C" {
#endif

/* math.h */
double acos(double);
double asin(double);
double atan(double);
double atan2(double, double);
double ceil(double);
double copysign(double, double);
double cos(double);
double cosh(double); // not in nsmbw
double exp(double); // not in nsmbw
inline double fabs(double x) { return __fabs(x); }
double floor(double);
double fmod(double, double);
double frexp(double, int *);
double ldexp(double, int);
double log(double); // not in nsmbw?
double log10(double);
double modf(double, double *);
double pow(double, double);
double sin(double);
double sinh(double); // not in nsmbw
double sqrt(double);
double tan(double);
double tanh(double); // not in nsmbw

extern f32 NAN;

/* stdarg.h */
typedef struct va_list_struct {
	char gpr, fpr, reserved[2], *input_arg_area, *reg_save_area;
} va_list[1];
extern void *__va_arg(void *, int);
#define va_start(ap, last) (__builtin_va_info(&ap))
#define va_arg(ap, type) (*((type *)__va_arg(ap, _var_arg_typeof(type))))
#define va_end(ap) ((void)0)

/* stdio.h */
int snprintf(char *, size_t, const char *, ...);
int sprintf(char *, const char *, ...);
int vsnprintf(char *, size_t, const char *, va_list);
int vsprintf(char *, const char *, va_list);

/* stdlib.h */
int abs(int);
long labs(long);
int atoi(const char *str);

/* string.h */
void *memchr(const void *, int, size_t);
int memcmp(const void *, const void *, size_t);
void *memcpy(void *, const void *, size_t);
void *memmove(void *, const void *, size_t);
void *memset(void *, int, size_t);
char *strcpy(char *, const char *);
char *strncpy(char *, const char *, size_t);
char *strcat(char *, const char *);
char *strncat(char *, const char *, size_t);
size_t strlen(const char *);
int strcmp(const char *, const char *);
int strncmp(const char *, const char *, size_t);
int stricmp(const char *, const char *); // not standard but nsmbw's got it anyway!
char *strchr(const char *, int);
char *strrchr(const char *, int);
char *strstr(const char *, const char *);

/* wchar.h */
int vswprintf(wchar_t *, size_t, const wchar_t *, va_list);
wchar_t *wcschr(const wchar_t *, wchar_t);
int wcscmp(const wchar_t *, const wchar_t *);
wchar_t *wcscpy(wchar_t *, const wchar_t *);
wchar_t *wcsncpy(wchar_t *, const wchar_t *, size_t);
size_t wcslen(const wchar_t *);
wchar_t *wmemchr(const wchar_t, wchar_t, size_t);
wchar_t *wmemcpy(wchar_t *, const wchar_t *, size_t);

#ifdef __cplusplus
}
#endif

#endif
