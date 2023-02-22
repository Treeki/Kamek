..\cw\mwcceppc -I- -i ../k_stdlib -Cpp_exceptions off -enum int -Os -use_lmw_stmw on -fp hard -rostr -sdata 0 -sdata2 0 -c -o 1-nsmbw-osreport.o 1-nsmbw-osreport.cpp
..\Kamek\bin\Debug\net6.0\Kamek 1-nsmbw-osreport.o -static=0x80341E68 -externals=externals-nsmbw-eu-v1.txt -output-riiv=1-loader.xml -output-gecko=1-loader.txt -output-code=1-loader.bin
..\Kamek\bin\Debug\net6.0\Kamek 1-nsmbw-osreport.o -dynamic -externals=externals-nsmbw-eu-v1.txt -versions=versions-nsmbw.txt -output-kamek=1-dynamic.$KV$.bin


