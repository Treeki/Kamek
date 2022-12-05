..\cw\mwasmeppc -I- -i ../k_stdlib -c -o 2-assembly.o 2-assembly.S
..\Kamek\bin\Debug\net6.0\Kamek 2-assembly.o -static=0x80341E68 -externals=externals-nsmbw-eu-v1.txt -output-riiv=2-loader.xml -output-gecko=2-loader.txt -output-code=2-loader.bin
..\Kamek\bin\Debug\net6.0\Kamek 2-assembly.o -dynamic -externals=externals-nsmbw-eu-v1.txt -versions=versions-nsmbw.txt -output-kamek=2-dynamic.$KV$.bin


