#include "kamekLoader.h"

int loadIntoNSMBW();
kmCondWritePointer(0x80328478, 0x8015BC60, loadIntoNSMBW); // EU
kmCondWritePointer(0x80328130, 0x8015BB20, loadIntoNSMBW); // US
kmCondWritePointer(0x80327E98, 0x8015B930, loadIntoNSMBW); // JP
kmCondWritePointer(0x80334E60, 0x8015C060, loadIntoNSMBW); // KR
kmCondWritePointer(0x80333218, 0x8015C060, loadIntoNSMBW); // TW

typedef void *(*EGG_Heap_Alloc_t) (u32 size, s32 align, void *heap);
typedef void (*EGG_Heap_Free_t) (void *buffer, void *heap);

struct loaderFunctionsEx {
	loaderFunctions base;
	EGG_Heap_Alloc_t eggAlloc;
	EGG_Heap_Free_t eggFree;
	void **gameHeapPtr;
	void **archiveHeapPtr;
};

void *allocAdapter(u32 size, bool isForCode, const loaderFunctions *funcs) {
	const loaderFunctionsEx *funcsEx = (const loaderFunctionsEx *)funcs;
	void **heapPtr = isForCode ? funcsEx->gameHeapPtr : funcsEx->archiveHeapPtr;
	return funcsEx->eggAlloc(size, 0x20, *heapPtr);
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
	(void **) 0x80377F48,
	(void **) 0x8042A72C
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
	(void **) 0x80377C48,
	(void **) 0x8042A44C
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
	(void **) 0x803779C8,
	(void **) 0x8042A16C
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
	(void **) 0x80384948,
	(void **) 0x804370EC
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
	(void **) 0x80382D48,
	(void **) 0x804354EC
};

void unknownVersion()
{
	// can't do much here!
	// we can't output a message on screen without a valid OSFatal addr
	for (;;);
}

int loadIntoNSMBW()
{
	int version = 0, region = 0;

	switch (*((u32*)0x800CF6CC))
	{
		case 0x40820030: region = 'P'; version = 1; break;
		case 0x40820038: region = 'P'; version = 2; break;
		case 0x48000465: region = 'E'; version = 1; break;
		case 0x2C030000: region = 'E'; version = 2; break;
		case 0x480000B4: region = 'J'; version = 1; break;
		case 0x4082000C: region = 'J'; version = 2; break;
		case 0x38A00001:
			switch (*((u8*)0x8000423A))
			{
				case 0xC8: region = 'K'; break;
				case 0xAC: region = 'W'; break;
				default: unknownVersion();
			}
			break;
		default: unknownVersion();
	}


	// choose functions
	// (these are all the same in v1 and v2, thankfully)
	const loaderFunctions *funcs = NULL;
	switch (region)
	{
		case 'P': funcs = &functions_p.base; break;
		case 'E': funcs = &functions_e.base; break;
		case 'J': funcs = &functions_j.base; break;
		case 'K': funcs = &functions_k.base; break;
		case 'W': funcs = &functions_w.base; break;
	}

	char path[64];
	if (version == 0)
		funcs->sprintf(path, "/engine.%c.bin", region);
	else
		funcs->sprintf(path, "/engine.%c%d.bin", region, version);
	loadKamekBinaryFromDisc(funcs, path);

	return 1;
}

