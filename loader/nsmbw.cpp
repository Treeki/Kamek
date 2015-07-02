#include "kamekLoader.h"

void loadIntoNSMBW();
kmCondWrite32(0x80328478, 0x8015BC60, loadIntoNSMBW); // EU
kmCondWrite32(0x80328130, 0x8015BB20, loadIntoNSMBW); // US
kmCondWrite32(0x80327E98, 0x8015B930, loadIntoNSMBW); // JP


const loaderFunctions functions_eu = {
	(OSReport_t) 0x8015F870,
	(OSFatal_t) 0x801AF710,
	(DVDConvertPathToEntrynum_t) 0x801CA7C0,
	(DVDFastOpen_t) 0x801CAAD0,
	(DVDReadPrio_t) 0x801CAC60,
	(DVDClose_t) 0x801CAB40,
	(sprintf_t) 0x802E1ACC,
};
const loaderFunctions functions_us = {
	(OSReport_t) 0x8015F830,
	(OSFatal_t) 0x801AF5D0,
	(DVDConvertPathToEntrynum_t) 0x801CA680,
	(DVDFastOpen_t) 0x801CA990,
	(DVDReadPrio_t) 0x801CAB20,
	(DVDClose_t) 0x801CAA00,
	(sprintf_t) 0x802E17DC,
};
const loaderFunctions functions_jp = {
	(OSReport_t) 0x8015F540,
	(OSFatal_t) 0x801AF3E0,
	(DVDConvertPathToEntrynum_t) 0x801CA490,
	(DVDFastOpen_t) 0x801CA7A0,
	(DVDReadPrio_t) 0x801CA930,
	(DVDClose_t) 0x801CA810,
	(sprintf_t) 0x802E15EC,
};

void unknownVersion()
{
	// can't do much here!
	// we can't output a message on screen without a valid OSFatal addr
	for (;;);
}

void loadIntoNSMBW()
{
	int version = 0, region = 0;

	u16 ident1 = *((u16*)0x80768D52);
	u16 ident2 = *((u16*)0x80768D92);

	if (ident1 == 0x14)
	{
		version = 2;
		switch (ident2)
		{
			case 0x6DA1: region = 'P'; break;
			case 0x6C61: region = 'E'; break;
			case 0x6A71: region = 'J'; break;
			default: unknownVersion();
		}
	}
	else
	{
		version = 1;
		switch (ident1)
		{
			case 0x6DE1: region = 'P'; break;
			case 0x6CA1: region = 'E'; break;
			case 0x6AB1: region = 'J'; break;
			default: unknownVersion();
		}
	}


	// choose functions
	// (these are all the same in v1 and v2, thankfully)
	const loaderFunctions *funcs = NULL;
	switch (region)
	{
		case 'P': funcs = &functions_eu; break;
		case 'E': funcs = &functions_us; break;
		case 'J': funcs = &functions_jp; break;
	}

	char path[64];
	funcs->sprintf(path, "/engine.%c%d.bin", region, version);
	loadKamekBinary(funcs, path);
}

