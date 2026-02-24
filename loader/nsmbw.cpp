#include "kamekLoader.h"

// based on the "First Stage Loader" by CLF78

typedef void *(*EGG_Heap_Alloc_t) (u32 size, s32 align, void *heap);
typedef void (*EGG_Heap_Free_t) (void *buffer, void *heap);
typedef void *(*memcpy_t) (void *dest, const void *src, size_t count);
typedef void (*flush_cache_t) (void*, size_t);

struct loaderFunctionsEx {
	loaderFunctions base;
	EGG_Heap_Alloc_t eggAlloc;
	EGG_Heap_Free_t eggFree;
	memcpy_t memcpy;
	flush_cache_t flushCache;
	void **gameHeapPtr;
	void **archiveHeapPtr;
	u32* bcaCheck;
	u32* gameInitTable;
};

// store the starting address for custom code at 0x800014e0 for later use
extern u32 codeAddr:0x800014e0;

void *allocAdapter(u32 size, bool isForCode, const loaderFunctions *funcs) {
	const loaderFunctionsEx *funcsEx = (const loaderFunctionsEx *)funcs;
	void **heapPtr = isForCode ? funcsEx->gameHeapPtr : funcsEx->archiveHeapPtr;
	void *text = funcsEx->eggAlloc(size, 0x20, *heapPtr);
	if (isForCode) {
		funcs->OSReport("Code start at %p\n", text);
		codeAddr = (u32)text;
	}
	return text;
}
void freeAdapter(void *buffer, bool isForCode, const loaderFunctions *funcs) {
	const loaderFunctionsEx *funcsEx = (const loaderFunctionsEx *)funcs;
	void **heapPtr = isForCode ? funcsEx->gameHeapPtr : funcsEx->archiveHeapPtr;
	funcsEx->eggFree(buffer, *heapPtr);
}


const loaderFunctionsEx functions_p = {
	{(OSReport_t) 0x8015F870,
	(OSFatal_t) 0x801AF710,
	(DVDConvertPathToEntrynum_t) 0x801CA7C0,
	(DVDFastOpen_t) 0x801CAAD0,
	(DVDReadPrio_t) 0x801CAC60,
	(DVDClose_t) 0x801CAB40,
	(sprintf_t) 0x802E1ACC,
	allocAdapter,
	freeAdapter},
	(EGG_Heap_Alloc_t) 0x802B8E00,
	(EGG_Heap_Free_t) 0x802B90B0,
	(memcpy_t) 0x80004364,
	(flush_cache_t) 0x80004330,
	(void **) 0x80377F48,
	(void **) 0x8042A72C,
	(u32*) 0x800CA0B8,
	(u32*) 0x80328428
};
const loaderFunctionsEx functions_e = {
	{(OSReport_t) 0x8015F730,
	(OSFatal_t) 0x801AF5D0,
	(DVDConvertPathToEntrynum_t) 0x801CA680,
	(DVDFastOpen_t) 0x801CA990,
	(DVDReadPrio_t) 0x801CAB20,
	(DVDClose_t) 0x801CAA00,
	(sprintf_t) 0x802E17DC,
	allocAdapter,
	freeAdapter},
	(EGG_Heap_Alloc_t) 0x802B8CC0,
	(EGG_Heap_Free_t) 0x802B8F70,
	(memcpy_t) 0x80004364,
	(flush_cache_t) 0x80004330,
	(void **) 0x80377C48,
	(void **) 0x8042A44C,
	(u32*) 0x800C9FC8,
	(u32*) 0x803280E0

};
const loaderFunctionsEx functions_j = {
	{(OSReport_t) 0x8015F540,
	(OSFatal_t) 0x801AF3E0,
	(DVDConvertPathToEntrynum_t) 0x801CA490,
	(DVDFastOpen_t) 0x801CA7A0,
	(DVDReadPrio_t) 0x801CA930,
	(DVDClose_t) 0x801CA810,
	(sprintf_t) 0x802E15EC,
	allocAdapter,
	freeAdapter},
	(EGG_Heap_Alloc_t) 0x802B8AD0,
	(EGG_Heap_Free_t) 0x802B8D80,
	(memcpy_t) 0x80004364,
	(flush_cache_t) 0x80004330,
	(void **) 0x803779C8,
	(void **) 0x8042A16C,
	(u32*) 0x800C9F48,
	(u32*) 0x80327E48
};
const loaderFunctionsEx functions_k = {
	{(OSReport_t) 0x8015FC70,
	(OSFatal_t) 0x801AFB10,
	(DVDConvertPathToEntrynum_t) 0x801CABC0,
	(DVDFastOpen_t) 0x801CAED0,
	(DVDReadPrio_t) 0x801CB060,
	(DVDClose_t) 0x801CAF40,
	(sprintf_t) 0x802E1D1C,
	allocAdapter,
	freeAdapter},
	(EGG_Heap_Alloc_t) 0x802B9200,
	(EGG_Heap_Free_t) 0x802B94B0,
	(memcpy_t) 0x80004364,
	(flush_cache_t) 0x80004330,
	(void **) 0x80384948,
	(void **) 0x804370EC,
	(u32*) 0x800CA0D8,
	(u32*) 0x80334E10
};
const loaderFunctionsEx functions_w = {
	{(OSReport_t) 0x8015FC70,
	(OSFatal_t) 0x801AFB10,
	(DVDConvertPathToEntrynum_t) 0x801CABC0,
	(DVDFastOpen_t) 0x801CAED0,
	(DVDReadPrio_t) 0x801CB060,
	(DVDClose_t) 0x801CAF40,
	(sprintf_t) 0x802E1D1C,
	allocAdapter,
	freeAdapter},
	(EGG_Heap_Alloc_t) 0x802B9200,
	(EGG_Heap_Free_t) 0x802B94B0,
	(memcpy_t) 0x80004364,
	(flush_cache_t) 0x80004330,
	(void **) 0x80382D48,
	(void **) 0x804354EC,
	(u32*) 0x800CA0D8,
	(u32*) 0x803331D0
};

const loaderFunctionsEx functions_c = {
	{(OSReport_t) 0x80161a90,
	(OSFatal_t) 0x801b1930,
	(DVDConvertPathToEntrynum_t) 0x801cc9e0,
	(DVDFastOpen_t) 0x801cccf0,
	(DVDReadPrio_t) 0x801cce80,
	(DVDClose_t) 0x801ccd60,
	(sprintf_t) 0x802e4df8,
	allocAdapter,
	freeAdapter},
	(EGG_Heap_Alloc_t) 0x802bb360,
	(EGG_Heap_Free_t) 0x802bb610,
	(memcpy_t) 0x80004364,
	(flush_cache_t) 0x80004330,
	(void **) 0x8037d4c8,
	(void **) 0x8042fccc,
	(u32*) 0x800ca2d8,
	(u32*) 0x8032d2f8
};

void unknownVersion() {
	// can't do much here!
	// we can't output a message on screen without a valid OSFatal addr;
	// all we can really do is set the screen to solid red before we die
	// (note: Dolphin Emulator currently ignores this)
	unsigned int *HW_VISOLID = (unsigned int*)0xcd000024;
	*HW_VISOLID = 0x5aef5101;

	for (;;);
}

struct versionInfo {
	char region;
	u8 revision;
};

static versionInfo sVersionInfo;
static const loaderFunctionsEx *sFuncs;

versionInfo checkVersion() {
	versionInfo version;

	// default is PALv0
	version.region = 'P';
	version.revision = 0;
	switch (*((u32*)0x800CF6CC))
	{
		case 0x40820030: version.region = 'P'; version.revision = 1; break;
		case 0x40820038: version.region = 'P'; version.revision = 2; break;
		case 0x48000465: version.region = 'E'; version.revision = 1; break;
		case 0x2C030000: version.region = 'E'; version.revision = 2; break;
		case 0x480000B4: version.region = 'J'; version.revision = 1; break;
		case 0x4082000C: version.region = 'J'; version.revision = 2; break;
		case 0x38A00001:
			switch (*((u8*)0x8000423A))
			{
				case 0xC8: version.region = 'K'; break;
				case 0xAC: version.region = 'W'; break;
				default: unknownVersion();
			}
			break;
		case 0x4182000c: version.region = 'C'; break;
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
	// set version before we do anything
	sVersionInfo = checkVersion();

	// choose functions
	// (these are all the same in v1 and v2, thankfully)
	switch (sVersionInfo.region) {
		case 'P': sFuncs = &functions_p; break;
		case 'E': sFuncs = &functions_e; break;
		case 'J': sFuncs = &functions_j; break;
		case 'K': sFuncs = &functions_k; break;
		case 'W': sFuncs = &functions_w; break;
		case 'C': sFuncs = &functions_c; break;
	}

	// report some info
	sFuncs->base.OSReport("<< NSMBW - LOADER 	release build: " __DATE__ " " __TIME__ " (0x4302_145) >>\n");
	// sFuncs->base.OSReport("found region %c%d!\n", sVersionInfo.region, sVersionInfo.revision);

	// reset the AI control register because libogc doesn't reset this right,
	// so on console the SDK can't initialize audio correctly coming from the Riivolution channel
	aiControl = 0;

	// remove the BCA check
	// this not only fixes some USB loaders running mods if the game id is changed
	// but also gains some of the time lost due to kamek binary injections
	*sFuncs->bcaCheck = 0x60000000;
	// we call __flush_cache to remove any stale instructions after writing to RAM
	sFuncs->flushCache(sFuncs->gameInitTable, 4);

	// modify gameInitTable to load rels earlier & load kamek binary
	u32 buffer[20];
	sFuncs->memcpy(&buffer, sFuncs->gameInitTable, 80);

	// set rel loading functions as first entries in the table
	sFuncs->gameInitTable[0] = buffer[15];
	sFuncs->gameInitTable[1] = buffer[16];
	sFuncs->gameInitTable[2] = buffer[17];

	// kamek binary loading as fourth entry
	sFuncs->gameInitTable[3] = (u32)&loadBinary;

	// set all the previous functions
	sFuncs->memcpy(&sFuncs->gameInitTable[4], &buffer, 60);

	// set the remaining two functions
	sFuncs->gameInitTable[19] = buffer[18];
	sFuncs->gameInitTable[20] = buffer[19];

	sFuncs->flushCache(sFuncs->gameInitTable, 80);
}

kmBranch(0x80004320, loadIntoNSMBW);