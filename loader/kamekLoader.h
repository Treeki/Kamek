#include <kamek.h>
#include <kamek_sdk.h>

struct DVDHandle;

struct loaderFunctions;

typedef void (*OSReport_t) (const char *str, ...);
typedef void (*OSFatal_t) (u32 *fg, u32 *bg, const char *str, ...);
typedef int (*DVDConvertPathToEntrynum_t) (const char *path);
typedef bool (*DVDFastOpen_t) (int entrynum, DVDHandle *handle);
typedef int (*DVDReadPrio_t) (DVDHandle *handle, void *buffer, int length, int offset, int unk);
typedef bool (*DVDClose_t) (DVDHandle *handle);
typedef int (*sprintf_t) (char *str, const char *format, ...);
typedef void *(*memcpy_t) (void *dest, const void *src, size_t count);
typedef void *(*KamekAlloc_t) (u32 size, bool isForCode, const loaderFunctions *funcs);
typedef void (*KamekFree_t) (void *buffer, bool isForCode, const loaderFunctions *funcs);


struct loaderFunctions {
    OSReport_t OSReport;
    OSFatal_t OSFatal;
    DVDConvertPathToEntrynum_t DVDConvertPathToEntrynum;
    DVDFastOpen_t DVDFastOpen;
    DVDReadPrio_t DVDReadPrio;
    DVDClose_t DVDClose;
    sprintf_t sprintf;
    memcpy_t memcpy;
    KamekAlloc_t kamekAlloc;
    KamekFree_t kamekFree;
};


void kamekError(const loaderFunctions *funcs, const char *str);
void loadKamekBinaryFromDisc(const loaderFunctions *funcs, const char *path);
