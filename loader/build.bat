..\cw\mwcceppc -i . -I- -i ../k_stdlib -Cpp_exceptions off -enum int -Os -use_lmw_stmw on -fp hard -rostr -sdata 0 -sdata2 0 -c -o kamekLoader.o kamekLoader.cpp
..\cw\mwcceppc -i . -I- -i ../k_stdlib -Cpp_exceptions off -enum int -Os -use_lmw_stmw on -fp hard -rostr -sdata 0 -sdata2 0 -c -o nsmbw.o nsmbw.cpp

..\Kamek\bin\Debug\net6.0\Kamek kamekLoader.o nsmbw.o -static=0x80001900 -output-code=loader.bin -output-riiv=loader.xml -valuefile=loader.bin -input-dol=nsmbw_pal.dol -output-dol=nsmbw_pal_kamek.dol
..\Kamek\bin\Debug\net6.0\Kamek kamekLoader.o nsmbw.o -static=0x80001900 -input-dol=nsmbw_ntsc.dol -output-dol=nsmbw_ntsc_kamek.dol

