CW_PATH=../cw
CPPFILES="kamekLoader nsmbw"

CC=$CW_PATH/mwcceppc
CFLAGS="-i . -I- -i ../k_stdlib -Cpp_exceptions off -enum int -Os -use_lmw_stmw on -fp hard -rostr -sdata 0 -sdata2 0"

for i in $CPPFILES
do
	echo Compiling $i.cpp...
	$CC $CFLAGS -c -o $i.o $i.cpp
done

echo Linking...

../Kamek/bin/Debug/net6.0/Kamek kamekLoader.o nsmbw.o -static=0x80001900 -output-code=loader.bin -output-riiv=loader.xml -use-valuefile -input-dol=nsmbw_pal.dol -output-dol=nsmbw_pal_kamek.dol
#../Kamek/bin/Debug/net6.0/Kamek kamekLoader.o nsmbw.o -dynamic -output-code=loader.bin -output-kamek=loader.kamek -output-riiv=loader.xml

