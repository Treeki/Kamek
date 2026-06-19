#include "kamekLoader.h"

// originally by Ninji, with improvements by CLF78, Ryguy, and RoadrunnerWMC

#define STRINGIFY_(x) #x
#define STRINGIFY(x) STRINGIFY_(x)

typedef void *(*EGG_Heap_alloc_t) (u32 size, s32 align, void *heap);
typedef void (*EGG_Heap_free_t) (void *buffer, void *heap);
typedef void (*flush_cache_t) (void *buffer, size_t size);

struct loaderFunctionsEx {
    loaderFunctions base;
    EGG_Heap_alloc_t EGG_Heap_alloc;
    EGG_Heap_free_t EGG_Heap_free;
    memcpy_t memcpy;
    flush_cache_t __flush_cache;
    void **mHeap_g_gameHeaps;
    void **mHeap_g_archiveHeap;
    u32* bcaCheck;
    u32* myBackGround_PhaseMethod;
};

// store the base address for custom code at 0x800014E0, for optional later use (e.g. for custom exception handlers)
extern u32 codeAddr:0x800014E0;

void *allocAdapter(u32 size, bool isForCode, const loaderFunctions *funcs) {
    const loaderFunctionsEx *funcsEx = (const loaderFunctionsEx *)funcs;
    void **heapPtr = isForCode ? funcsEx->mHeap_g_gameHeaps : funcsEx->mHeap_g_archiveHeap;
    void *text = funcsEx->EGG_Heap_alloc(size, 0x20, *heapPtr);
    if (isForCode)
        codeAddr = (u32)text;
    return text;
}
void freeAdapter(void *buffer, bool isForCode, const loaderFunctions *funcs) {
    const loaderFunctionsEx *funcsEx = (const loaderFunctionsEx *)funcs;
    void **heapPtr = isForCode ? funcsEx->mHeap_g_gameHeaps : funcsEx->mHeap_g_archiveHeap;
    funcsEx->EGG_Heap_free(buffer, *heapPtr);
}


const loaderFunctionsEx functions_P = {
    {(OSReport_t) 0x8015F870,
    (OSFatal_t) 0x801AF710,
    (DVDConvertPathToEntrynum_t) 0x801CA7C0,
    (DVDFastOpen_t) 0x801CAAD0,
    (DVDReadPrio_t) 0x801CAC60,
    (DVDClose_t) 0x801CAB40,
    (sprintf_t) 0x802E1ACC,
    (memcpy_t) 0x80004364,
    allocAdapter,
    freeAdapter},
    (EGG_Heap_alloc_t) 0x802B8E00,
    (EGG_Heap_free_t) 0x802B90B0,
    (flush_cache_t) 0x80004330,
    (void **) 0x80377F48,
    (void **) 0x8042A72C,
    (u32*) 0x800CA0B8,
    (u32*) 0x80328428
};
const loaderFunctionsEx functions_E = {
    {(OSReport_t) 0x8015F730,
    (OSFatal_t) 0x801AF5D0,
    (DVDConvertPathToEntrynum_t) 0x801CA680,
    (DVDFastOpen_t) 0x801CA990,
    (DVDReadPrio_t) 0x801CAB20,
    (DVDClose_t) 0x801CAA00,
    (sprintf_t) 0x802E17DC,
    (memcpy_t) 0x80004364,
    allocAdapter,
    freeAdapter},
    (EGG_Heap_alloc_t) 0x802B8CC0,
    (EGG_Heap_free_t) 0x802B8F70,
    (flush_cache_t) 0x80004330,
    (void **) 0x80377C48,
    (void **) 0x8042A44C,
    (u32*) 0x800C9FC8,
    (u32*) 0x803280E0

};
const loaderFunctionsEx functions_J = {
    {(OSReport_t) 0x8015F540,
    (OSFatal_t) 0x801AF3E0,
    (DVDConvertPathToEntrynum_t) 0x801CA490,
    (DVDFastOpen_t) 0x801CA7A0,
    (DVDReadPrio_t) 0x801CA930,
    (DVDClose_t) 0x801CA810,
    (sprintf_t) 0x802E15EC,
    (memcpy_t) 0x80004364,
    allocAdapter,
    freeAdapter},
    (EGG_Heap_alloc_t) 0x802B8AD0,
    (EGG_Heap_free_t) 0x802B8D80,
    (flush_cache_t) 0x80004330,
    (void **) 0x803779C8,
    (void **) 0x8042A16C,
    (u32*) 0x800C9F48,
    (u32*) 0x80327E48
};
const loaderFunctionsEx functions_K = {
    {(OSReport_t) 0x8015FC70,
    (OSFatal_t) 0x801AFB10,
    (DVDConvertPathToEntrynum_t) 0x801CABC0,
    (DVDFastOpen_t) 0x801CAED0,
    (DVDReadPrio_t) 0x801CB060,
    (DVDClose_t) 0x801CAF40,
    (sprintf_t) 0x802E1D1C,
    (memcpy_t) 0x80004364,
    allocAdapter,
    freeAdapter},
    (EGG_Heap_alloc_t) 0x802B9200,
    (EGG_Heap_free_t) 0x802B94B0,
    (flush_cache_t) 0x80004330,
    (void **) 0x80384948,
    (void **) 0x804370EC,
    (u32*) 0x800CA0D8,
    (u32*) 0x80334E10
};
const loaderFunctionsEx functions_W = {
    {(OSReport_t) 0x8015FC70,
    (OSFatal_t) 0x801AFB10,
    (DVDConvertPathToEntrynum_t) 0x801CABC0,
    (DVDFastOpen_t) 0x801CAED0,
    (DVDReadPrio_t) 0x801CB060,
    (DVDClose_t) 0x801CAF40,
    (sprintf_t) 0x802E1D1C,
    (memcpy_t) 0x80004364,
    allocAdapter,
    freeAdapter},
    (EGG_Heap_alloc_t) 0x802B9200,
    (EGG_Heap_free_t) 0x802B94B0,
    (flush_cache_t) 0x80004330,
    (void **) 0x80382D48,
    (void **) 0x804354EC,
    (u32*) 0x800CA0D8,
    (u32*) 0x803331D0
};

const loaderFunctionsEx functions_C = {
    {(OSReport_t) 0x80161A90,
    (OSFatal_t) 0x801B1930,
    (DVDConvertPathToEntrynum_t) 0x801CC9E0,
    (DVDFastOpen_t) 0x801CCCF0,
    (DVDReadPrio_t) 0x801CCE80,
    (DVDClose_t) 0x801CCD60,
    (sprintf_t) 0x802E4DF8,
    (memcpy_t) 0x80004364,
    allocAdapter,
    freeAdapter},
    (EGG_Heap_alloc_t) 0x802BB360,
    (EGG_Heap_free_t) 0x802BB610,
    (flush_cache_t) 0x80004330,
    (void **) 0x8037D4C8,
    (void **) 0x8042FCCC,
    (u32*) 0x800CA2D8,
    (u32*) 0x8032D2F8
};

void unknownVersion() {
    // can't do much here!
    // we can't output a message on screen without a valid OSFatal addr;
    // all we can really do is set the screen to solid red before we die
    // (note: Dolphin Emulator currently ignores this)
    unsigned int *HW_VISOLID = (unsigned int*)0xCD000024;
    *HW_VISOLID = 0x5AEF5101;

    for (;;);
}

union versionInfo {
    struct {
        char region;
        u8 revision;
    };
    u16 pair;
};

static versionInfo sVersionInfo;
static const loaderFunctionsEx *sFuncs;

versionInfo checkVersion() {
    versionInfo version;

    switch (*((u32*)0x800CF6CC))
    {
        case 0x40820030: version.pair = 'P\1'; break;
        case 0x40820038: version.pair = 'P\2'; break;
        case 0x48000465: version.pair = 'E\1'; break;
        case 0x2C030000: version.pair = 'E\2'; break;
        case 0x480000B4: version.pair = 'J\1'; break;
        case 0x4082000C: version.pair = 'J\2'; break;
        case 0x38A00001:
            switch (*((u8*)0x8000423A))
            {
                case 0xC8: version.region = 'K'; break;
                case 0xAC: version.region = 'W'; break;
                default: unknownVersion();
            }
            break;
        case 0x4182000C: version.region = 'C'; break;
        default: unknownVersion();
    }

    return version;
}

int loadBinary() {
    char path[64];
    if (sVersionInfo.revision == 0)
        sFuncs->base.sprintf(path, "/Code/%c.bin", sVersionInfo.region);
    else
        sFuncs->base.sprintf(path, "/Code/%c%d.bin", sVersionInfo.region, sVersionInfo.revision);
    loadKamekBinaryFromDisc(&sFuncs->base, path);

    return 1;
}

extern vu32 aiControl:0xCD006C00;

void loadIntoNSMBW() {
    // set version before we do anything else
    sVersionInfo = checkVersion();

    // choose functions
    // (these are all the same in v1 and v2, thankfully)
    switch (sVersionInfo.region) {
        case 'P': sFuncs = &functions_P; break;
        case 'E': sFuncs = &functions_E; break;
        case 'J': sFuncs = &functions_J; break;
        case 'K': sFuncs = &functions_K; break;
        case 'W': sFuncs = &functions_W; break;
        case 'C': sFuncs = &functions_C; break;
    }

    // report some info
    sFuncs->base.OSReport("<< KAMEK - LOADER \trelease build: " __DATE__ " " __TIME__ " (" STRINGIFY(__CWCC__) "_" STRINGIFY(__CWBUILD__) ") >>\n");
    // sFuncs->base.OSReport("Detected game version: %c%d\n", sVersionInfo.region, sVersionInfo.revision);

    // reset the AI control register because libogc doesn't reset it right,
    // so on console the SDK can't initialize audio correctly when coming from the Riivolution channel
    aiControl = 0;

    // remove the BCA check
    // this not only fixes some USB loaders running mods if the game id is changed,
    // but also gains some of the time lost due to kamek binary injections
    *sFuncs->bcaCheck = 0x60000000;
    // we call __flush_cache to remove any stale instructions after writing to RAM
    sFuncs->__flush_cache(sFuncs->myBackGround_PhaseMethod, 4);

    // modify myBackGround_PhaseMethod to load rels earlier & load the kamek binary
    u32 temp[20];
    sFuncs->base.memcpy(&temp, sFuncs->myBackGround_PhaseMethod, 0x50);

    // set rel loading functions as the first entries in the table
    sFuncs->myBackGround_PhaseMethod[0] = temp[15];
    sFuncs->myBackGround_PhaseMethod[1] = temp[16];
    sFuncs->myBackGround_PhaseMethod[2] = temp[17];

    // set kamek binary loader as the fourth entry
    sFuncs->myBackGround_PhaseMethod[3] = (u32)&loadBinary;

    // set all the other functions
    sFuncs->base.memcpy(&sFuncs->myBackGround_PhaseMethod[4], &temp, 0x3C);
    sFuncs->myBackGround_PhaseMethod[19] = temp[18];
    sFuncs->myBackGround_PhaseMethod[20] = temp[19];

    sFuncs->__flush_cache(sFuncs->myBackGround_PhaseMethod, 0x50);
}

kmBranch(0x800042F4, loadIntoNSMBW);
