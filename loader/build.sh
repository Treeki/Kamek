CW_PATH=../cw
CPPFILES="kamekLoader nsmbw"

CC=$CW_PATH/mwcceppc
CFLAGS="-Cpp_exceptions off -enum int -Os -use_lmw_stmw on -fp hard -rostr -sdata 0 -sdata2 0"

for i in $CPPFILES
do
	echo Compiling $i.cpp...
	$CC $CFLAGS -c -o $i.o $i.cpp
done

echo Linking...

../Kamek/bin/Debug/Kamek kamekLoader.o nsmbw.o -static=0x80001800 -output-code=loader.bin -output-riiv=loader.xml
#../Kamek/bin/Debug/Kamek kamekLoader.o nsmbw.o -dynamic -output-code=loader.bin -output-kamek=loader.kamek -output-riiv=loader.xml

