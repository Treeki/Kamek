..\cw\mwcceppc.exe -i . -I- -i ../k_stdlib -Cpp_exceptions off -enum int -Os -use_lmw_stmw on -fp hard -rostr -sdata 0 -sdata2 0 -c -o shield-fix.o shield-fix.cpp
..\Kamek\bin\Debug\Kamek shield-fix.o -externals=addresses.txt -static=0x80002000 -output-code=loader.bin -output-riiv=loader.xml -output-gecko=gecko.txt -input-dol=cn_original.dol -output-dol=cn_patched.dol


