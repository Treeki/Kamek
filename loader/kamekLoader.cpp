#include "kamekLoader.h"

struct KBHeader {
	u32 magic1;
	u16 magic2;
	u16 version;
	u32 bssSize;
	u32 codeSize;
	u32 ctorStart;
	u32 ctorEnd;
	u32 _pad[2];
};


#define kAddr32 1
#define kAddr16Lo 4
#define kAddr16Hi 5
#define kAddr16Ha 6
#define kRel24 10
#define kWrite32 32
#define kWrite16 33
#define kWrite8 34
#define kCondWritePointer 35
#define kCondWrite32 36
#define kCondWrite16 37
#define kCondWrite8 38
#define kBranch 64
#define kBranchLink 65


void kamekError(const loaderFunctions *funcs, const char *str)
{
	u32 fg = 0xFFFFFFFF, bg = 0;
	funcs->OSFatal(&fg, &bg, str);
}


static inline u32 resolveAddress(u32 text, u32 address) {
	if (address & 0x80000000)
		return address;
	else
		return text + address;
}


#define kCommandHandler(name) \
	static inline const u8 *kHandle##name(const u8 *input, u32 text, u32 address)
#define kDispatchCommand(name) \
	case k##name: input = kHandle##name(input, text, address); break

kCommandHandler(Addr32) {
	u32 target = resolveAddress(text, *(const u32 *)input);
	*(u32 *)address = target;
	return input + 4;
}
kCommandHandler(Addr16Lo) {
	u32 target = resolveAddress(text, *(const u32 *)input);
	*(u16 *)address = target & 0xFFFF;
	return input + 4;
}
kCommandHandler(Addr16Hi) {
	u32 target = resolveAddress(text, *(const u32 *)input);
	*(u16 *)address = target >> 16;
	return input + 4;
}
kCommandHandler(Addr16Ha) {
	u32 target = resolveAddress(text, *(const u32 *)input);
	*(u16 *)address = target >> 16;
	if (target & 0x8000)
		*(u16 *)address += 1;
	return input + 4;
}
kCommandHandler(Rel24) {
	u32 target = resolveAddress(text, *(const u32 *)input);
	u32 delta = target - address;
	*(u32 *)address &= 0xFC000003;
	*(u32 *)address |= (delta & 0x3FFFFFC);
	return input + 4;
}
kCommandHandler(Write32) {
	u32 value = *(const u32 *)input;
	*(u32 *)address = value;
	return input + 4;
}
kCommandHandler(Write16) {
	u32 value = *(const u32 *)input;
	*(u16 *)address = value & 0xFFFF;
	return input + 4;
}
kCommandHandler(Write8) {
	u32 value = *(const u32 *)input;
	*(u8 *)address = value & 0xFF;
	return input + 4;
}
kCommandHandler(CondWritePointer) {
	u32 target = resolveAddress(text, *(const u32 *)input);
	u32 original = ((const u32 *)input)[1];
	if (*(u32 *)address == original)
		*(u32 *)address = target;
	return input + 8;
}
kCommandHandler(CondWrite32) {
	u32 value = *(const u32 *)input;
	u32 original = ((const u32 *)input)[1];
	if (*(u32 *)address == original)
		*(u32 *)address = value;
	return input + 8;
}
kCommandHandler(CondWrite16) {
	u32 value = *(const u32 *)input;
	u32 original = ((const u32 *)input)[1];
	if (*(u16 *)address == (original & 0xFFFF))
		*(u16 *)address = value & 0xFFFF;
	return input + 8;
}
kCommandHandler(CondWrite8) {
	u32 value = *(const u32 *)input;
	u32 original = ((const u32 *)input)[1];
	if (*(u8 *)address == (original & 0xFF))
		*(u8 *)address = value & 0xFF;
	return input + 8;
}
kCommandHandler(Branch) {
	*(u32 *)address = 0x48000000;
	return kHandleRel24(input, text, address);
}
kCommandHandler(BranchLink) {
	*(u32 *)address = 0x48000001;
	return kHandleRel24(input, text, address);
}


inline void cacheInvalidateAddress(u32 address) {
	register u32 addressRegister = address;
	asm {
		dcbst 0, addressRegister
		sync
		icbi 0, addressRegister
	}
}


void loadKamekBinary(const loaderFunctions *funcs, const void *binary, u32 binaryLength)
{
	const KBHeader *header = (const KBHeader *)binary;
	if (header->magic1 != 'Kame' || header->magic2 != 'k\0')
		kamekError(funcs, "FATAL ERROR: Corrupted file, please check your game's Kamek files");
	if (header->version != 2) {
		char err[512];
		funcs->sprintf(err, "FATAL ERROR: Incompatible file (version %d), please upgrade your Kamek Loader", header->version);
		kamekError(funcs, err);
	}
	
	funcs->OSReport("header: bssSize=%u, codeSize=%u, ctors=%u-%u\n",
		header->bssSize, header->codeSize, header->ctorStart, header->ctorEnd);

	u32 textSize = header->codeSize + header->bssSize;
	u32 text = (u32)funcs->kamekAlloc(textSize, true, funcs);
	if (!text)
		kamekError(funcs, "FATAL ERROR: Out of code memory");

	const u8 *input = ((const u8 *)binary) + sizeof(KBHeader);
	const u8 *inputEnd = ((const u8 *)binary) + binaryLength;
	u8 *output = (u8 *)text;
	
	// Create text + bss sections
	for (u32 i = 0; i < header->codeSize; i++){
		*output = *(input++);
		cacheInvalidateAddress((u32)(output++));
	}
	for (u32 i = 0; i < header->bssSize; i++){
		*output = 0;
		cacheInvalidateAddress((u32)(output++));
	}
	
	while (input < inputEnd) {
		u32 cmdHeader = *((u32 *)input);
		input += 4;

		u8 cmd = cmdHeader >> 24;
		u32 address = cmdHeader & 0xFFFFFF;
		if (address == 0xFFFFFE) {
			// Absolute address
			address = *((u32 *)input);
			input += 4;
		} else {
			// Relative address
			address += text;
		}

		switch (cmd) {
			kDispatchCommand(Addr32);
			kDispatchCommand(Addr16Lo);
			kDispatchCommand(Addr16Hi);
			kDispatchCommand(Addr16Ha);
			kDispatchCommand(Rel24);
			kDispatchCommand(Write32);
			kDispatchCommand(Write16);
			kDispatchCommand(Write8);
			kDispatchCommand(CondWritePointer);
			kDispatchCommand(CondWrite32);
			kDispatchCommand(CondWrite16);
			kDispatchCommand(CondWrite8);
			kDispatchCommand(Branch);
			kDispatchCommand(BranchLink);
			default:
				funcs->OSReport("Unknown command: %d\n", cmd);
		}
		
		cacheInvalidateAddress(address);
	}

	__sync();
	__isync();

	typedef void (*Func)(void);
	for (Func* f = (Func*)(text + header->ctorStart); f < (Func*)(text + header->ctorEnd); f++) {
		(*f)();
	}
}


void loadKamekBinaryFromDisc(const loaderFunctions *funcs, const char *path)
{
	funcs->OSReport("{Kamek by Treeki}\nLoading Kamek binary '%s'...\n", path);

	int entrynum = funcs->DVDConvertPathToEntrynum(path);
	if (entrynum < 0) {
		char err[512];
		funcs->sprintf(err, "FATAL ERROR: Failed to locate file on the disc: %s", path);
		kamekError(funcs, err);
	}

	DVDHandle handle;
	if (!funcs->DVDFastOpen(entrynum, &handle))
		kamekError(funcs, "FATAL ERROR: Failed to open file!");
	
	funcs->OSReport("DVD file located: addr=%p, size=%d\n", handle.address, handle.length);

	u32 length = handle.length, roundedLength = (handle.length + 0x1F) & ~0x1F;
	void *buffer = funcs->kamekAlloc(roundedLength, false, funcs);
	if (!buffer)
		kamekError(funcs, "FATAL ERROR: Out of file memory");
	
	funcs->DVDReadPrio(&handle, buffer, roundedLength, 0, 2);
	funcs->DVDClose(&handle);

	loadKamekBinary(funcs, buffer, handle.length);
	
	funcs->kamekFree(buffer, false, funcs);
	funcs->OSReport("All done!\n");
}
