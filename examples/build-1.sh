CW_PATH=../cw
CPPFILES=1-nsmbw-osreport

CC=$CW_PATH/mwcceppc
CFLAGS="-I- -i ../k_stdlib -Cpp_exceptions off -enum int -Os -use_lmw_stmw on -fp hard -rostr -sdata 0 -sdata2 0"

for i in $CPPFILES
do
	echo Compiling $i.cpp...
	$CC $CFLAGS -c -o $i.o $i.cpp
done

echo Linking...

#../Kamek/bin/Debug/Kamek 1-nsmbw-osreport.o -static=0x80001800 -externals=externals-nsmbw-eu-v1.txt -output-code=1-loader.bin -output-riiv=1-loader.xml
../Kamek/bin/Debug/Kamek 1-nsmbw-osreport.o -static=0x80341E68 -externals=externals-nsmbw-eu-v1.txt -output-riiv=1-loader.xml -output-gecko=1-loader.txt -output-code=1-loader.bin
../Kamek/bin/Debug/Kamek 1-nsmbw-osreport.o -dynamic -externals=externals-nsmbw-eu-v1.txt -versions=versions-nsmbw.txt -output-kamek=1-dynamic.\$KV\$.bin


