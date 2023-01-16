CW_PATH=../cw
CPPFILES="1-nsmbw-osreport"
ASMFILES=""

AS=$CW_PATH/mwasmeppc
ASMFLAGS="-I- -i ../k_stdlib"

CC=$CW_PATH/mwcceppc
CFLAGS="-I- -i ../k_stdlib -Cpp_exceptions off -enum int -Os -use_lmw_stmw on -fp hard -rostr -sdata 0 -sdata2 0"

OBJECTS=""

for i in $CPPFILES
do
	echo Compiling $i.cpp...
	OBJECTS="$OBJECTS $i.o"
	$CC $CFLAGS -c -o $i.o $i.cpp
done
for i in $ASMFILES
do
	echo Assembling $i.S...
	OBJECTS="$OBJECTS $i.o"
	$AS $ASMFLAGS -c -o $i.o $i.S
done

echo Linking...

../Kamek/bin/Debug/net6.0/Kamek $OBJECTS -static=0x80341E68 -externals=externals-nsmbw-eu-v1.txt -output-riiv=1-loader.xml -output-gecko=1-loader.txt -output-code=1-loader.bin
../Kamek/bin/Debug/net6.0/Kamek $OBJECTS -dynamic -externals=externals-nsmbw-eu-v1.txt -versions=versions-nsmbw.txt -output-kamek=1-dynamic.\$KV\$.bin


