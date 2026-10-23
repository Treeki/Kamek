from pathlib import Path
import shutil
import subprocess
import sys


CW = Path('../cw')
MWCCEPPC_EXE = CW / 'mwcceppc.exe'
MWASMEPPC_EXE = CW / 'mwasmeppc.exe'
CFLAGS = [
    '-I-',
    '-i', '../k_stdlib',
    '-Cpp_exceptions', 'off',
    '-enum', 'int',
    '-Os',
    '-use_lmw_stmw', 'on',
    '-fp', 'hard',
    '-rostr',
    '-sdata', '0',
    '-sdata2', '0',
]
ASMFLAGS = [
    '-I-',
    '-i', '../k_stdlib',
]
KAMEK_EXE = Path('../Kamek')


def compile_cpp(cpp_path: Path, o_path: Path) -> None:
    subprocess.run(['wine', str(MWCCEPPC_EXE), *CFLAGS, '-c', '-o', str(o_path), str(cpp_path)])
    if not o_path.is_file():
        raise ValueError(f'{MWCCEPPC_EXE} failed to produce {o_path}')


def compile_s(s_path: Path, o_path: Path) -> None:
    subprocess.run(['wine', str(MWASMEPPC_EXE), *ASMFLAGS, '-c', '-o', str(o_path), str(s_path)])
    if not o_path.is_file():
        raise ValueError(f'{MWASMEPPC_EXE} failed to produce {o_path}')


def kamek_static_link(o_files: list[Path], *, static: int, externals: Path, output_dir: Path) -> None:
    externals_arg = [f'-externals={externals}'] if externals.is_file() else []

    subprocess.run(
        [
            str(KAMEK_EXE),
            '-q',
            f'-static=0x{static:08x}',
            *externals_arg,
            f'-output-riiv={output_dir / "output-riiv.xml"}',
            f'-output-dolphin={output_dir / "output-dolphin.ini"}',
            f'-output-gecko={output_dir / "output-gecko.txt"}',
            f'-output-ar={output_dir / "output-ar.txt"}',
            f'-output-code={output_dir / "output-code.bin"}',
            f'-output-map={output_dir / "output-map-static.map"}',
            f'-input-dol=sample.dol',
            f'-output-dol={output_dir / "output-dol.dol"}',
            f'-input-alf=sample_104.alf',
            f'-output-alf={output_dir / "output-alf_104.alf"}',
            *(str(o) for o in o_files),
        ],
        check=True,
    )
    subprocess.run(
        [
            str(KAMEK_EXE),
            '-q',
            f'-static=0x{static:08x}',
            *externals_arg,
            f'-input-alf=sample_105.alf',
            f'-output-alf={output_dir / "output-alf_105.alf"}',
            *(str(o) for o in o_files),
        ],
        check=True,
    )


def kamek_dynamic_link(o_files: list[Path], *, externals: Path, versions: Path, output_dir: Path) -> None:
    externals_arg = [f'-externals={externals}'] if externals.is_file() else []

    subprocess.run(
        [
            str(KAMEK_EXE),
            '-q',
            '-dynamic',
            *externals_arg,
            f'-versions={versions}',
            f'-output-kamek={output_dir / "output-kamek.$KV$.bin"}',
            f'-output-map={output_dir / "output-map-dynamic.map"}',
            *(str(o) for o in o_files),
        ],
        check=True,
    )


def main() -> None:
    do_bless = '--bless' in sys.argv

    for test_dir in sorted(Path().iterdir()):
        if not test_dir.is_dir():
            continue
        if test_dir.name.startswith('_'):
            continue

        print(f'---- {test_dir.name} ----')

        bin_dir = test_dir / '_bin'
        out_dir = test_dir / '_out'

        shutil.rmtree(bin_dir, ignore_errors=True)
        bin_dir.mkdir()
        shutil.rmtree(out_dir, ignore_errors=True)
        out_dir.mkdir()

        o_files = []
        for cpp_file in test_dir.glob('*.cpp'):
            o_file = bin_dir / cpp_file.with_suffix('.o').name
            compile_cpp(cpp_file, o_file)
            o_files.append(o_file)
        for s_file in test_dir.glob('*.S'):
            o_file = bin_dir / s_file.with_suffix('.o').name
            compile_s(s_file, o_file)
            o_files.append(o_file)

        kamek_static_link(
            o_files,
            static=0x82000000,
            externals=test_dir / 'externals.txt',
            output_dir=out_dir,
        )
        kamek_dynamic_link(
            o_files,
            externals=test_dir / 'externals.txt',
            versions=Path('versions.txt'),
            output_dir=out_dir,
        )

        expected_dir = test_dir / 'expected'

        if do_bless:
            shutil.rmtree(expected_dir, ignore_errors=True)
            out_dir.rename(expected_dir)

        else:
            for expected_file in expected_dir.iterdir():
                actual_file = out_dir / expected_file.name

                if not actual_file.is_file():
                    raise ValueError(f'{actual_file} is missing')
                if expected_file.read_bytes() != actual_file.read_bytes():
                    raise ValueError(f'{expected_file} and {actual_file} are different!')

    if do_bless:
        print('All tests blessed')
    else:
        print('All tests passed successfully')


main()
