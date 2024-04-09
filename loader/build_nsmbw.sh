if [ "$1" = "--wine" ]; then
    CC='wine ../cw/mwcceppc.exe'
else
    CC='../cw/mwcceppc.exe'
fi

$CC -i . -I- -i ../k_stdlib -Cpp_exceptions off -enum int -Os -use_lmw_stmw on -fp hard -rostr -sdata 0 -sdata2 0 -c -o kamekLoader.o kamekLoader.cpp
$CC -i . -I- -i ../k_stdlib -Cpp_exceptions off -enum int -Os -use_lmw_stmw on -fp hard -rostr -sdata 0 -sdata2 0 -c -o nsmbw.o nsmbw.cpp

../Kamek/bin/Debug/net6.0/Kamek kamekLoader.o nsmbw.o -static=0x80001900 -output-code=loader.bin -output-riiv=loader.xml -valuefile=Code/loader.bin

# Or to inject directly into a DOL:
# ../Kamek/bin/Debug/net6.0/Kamek kamekLoader.o nsmbw.o -static=0x80001900 -input-dol=nsmbw.dol -output-dol=nsmbw_kamek.dol
