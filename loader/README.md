# The Kamek loader

*kamekLoader.cpp* in this directory is the runtime loader for the "Kamekfiles" that Kamek generates in `-dynamic` mode. The loader itself is built in *static* mode, since it needs to either be applied directly to a DOL file or built as a Riivolution memory patch.

Although the core loader code should work with most games, some game-specific code (a "bootstrap") is needed to invoke it. An example bootstrap for New Super Mario Bros. Wii is included.

This guide assumes you want to build the loader as a Riivolution patch, since that's the most common method by far. If you want to inject it directly into a DOL instead, use the `-input-dol` and `-output-dol` Kamek arguments instead of `-output-code` and `-output-riiv`.

## kamekLoader.cpp

This is the "core" part of the loader, which should work for most games.

The main API entrypoint is `loadKamekBinaryFromDisc()`, which loads a Kamekfile and applies its patches to the game. In addition to the Kamekfile path, it takes a structure of pointers to SDK functions it needs, which allows the bootstrap to determine their addresses at runtime if necessary.

## New Super Mario Bros. Wii bootstrap

*nsmbw.cpp* is the Kamek loader bootstrap for New Super Mario Bros. Wii, bridging the gap between *kamekLoader.cpp* and NSMBW's code. It's designed to support *all* versions of the game by detecting the one it finds itself in at runtime and invoking the loader appropriately.

Since you may want to adapt this bootstrap to a different game, here's an overview of the most important parts of its code:

- A set of `kmCondWritePointer()` hooks. These put a `loadIntoNSMBW()` function pointer (see below) into a table of function pointers that the game runs at startup, replacing an entry that (conveniently) originally pointed to an empty function. Multiple conditional hooks are needed since the table's address varies in different game versions.
- A "`loaderFunctionsEx`" struct that wraps the loader's `loaderFunctions` struct and adds some additional pointers useful for the NSMBW bootstrap.
- Some "adapters" that implement the `kamekAlloc()` and `kamekFree()` interfaces using appropriate functions from the game.
- A const `loaderFunctionsEx` instance for each game version, providing all the function addresses as hardcoded values.
- The `loadIntoNSMBW()` function, which:
    - Determines the running game version by checking some specific memory addresses,
    - Selects the appropriate `loaderFunctions` and Kamekfile path string, and
    - Calls `loadKamekBinaryFromDisc()`.

By default, it loads a Kamekfile from one of the following paths on the disc / in the Riivolution patch, depending on the detected game version:

- *Code/P1.bin* (international: SMNP v1)
- *Code/P2.bin* (international: SMNP v2)
- *Code/E1.bin* (North American: SMNE v1)
- *Code/E2.bin* (North American: SMNE v2)
- *Code/J1.bin* (Japanese: SMNJ v1)
- *Code/J2.bin* (Japanese: SMNJ v2)
- *Code/K.bin* (South Korean: SMNK)
- *Code/W.bin* (Taiwanese / Hong Kong: SMNW)

### Building the bootstrap and loader

To build the bootstrap and loader, place the CodeWarrior EXEs, DLLs, and *license.dat* in `../cw`, then run one of:

- `build_nsmbw.bat` (Windows)
- `build_nsmbw.sh` (sh, tries to invoke CodeWarrior without Wine -- appropriate for WSL)
- `build_nsmbw.sh --wine` (sh, uses Wine)

This will create two files:

- *loader.bin*: the loader code blob, to be placed at address 0x80001900 (by default).
- *loader.xml*: a **partial** Riivolution XML that includes a reference to *loader.bin* and the game hooks to invoke it.

Copy the contents of *loader.xml* into your Riivolution XML's `<patch>` tag, and put *loader.bin* in your Riivolution patch's *Code* directory alongside your Kamekfiles.
