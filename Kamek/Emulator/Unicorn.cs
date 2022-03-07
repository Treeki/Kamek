using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Kamek.Emulator {
	public class Unicorn : IDisposable {
		public const int REG_AH = 1;
		public const int REG_AL = 2;
		public const int REG_AX = 3;
		public const int REG_BH = 4;
		public const int REG_BL = 5;
		public const int REG_BP = 6;
		public const int REG_BPL = 7;
		public const int REG_BX = 8;
		public const int REG_CH = 9;
		public const int REG_CL = 10;
		public const int REG_CS = 11;
		public const int REG_CX = 12;
		public const int REG_DH = 13;
		public const int REG_DI = 14;
		public const int REG_DIL = 15;
		public const int REG_DL = 16;
		public const int REG_DS = 17;
		public const int REG_DX = 18;
		public const int REG_EAX = 19;
		public const int REG_EBP = 20;
		public const int REG_EBX = 21;
		public const int REG_ECX = 22;
		public const int REG_EDI = 23;
		public const int REG_EDX = 24;
		public const int REG_EFLAGS = 25;
		public const int REG_EIP = 26;
		public const int REG_ES = 27;
		public const int REG_ESI = 28;
		public const int REG_ESP = 29;
		public const int REG_FPSW = 30;
		public const int REG_FS = 31;
		public const int REG_GS = 32;
		public const int REG_IP = 33;

		public class UnicornException : Exception {
			private Error _error;
			private uint _eip;

			public UnicornException(IntPtr uc, Error error) {
				_error = error;
				uc_reg_read_u32(uc, REG_EIP, out _eip);
			}

			public override string ToString() {
				return $"{_error} (eip={_eip:X8})";
			}
		}

		public enum Arch {
			ARM = 1,
			ARM64,
			MIPS,
			X86,
			PPC,
			SPARC,
			M68K
		}

		[Flags]
		public enum Mode {
			Mode16 = 1 << 1,
			Mode32 = 1 << 2,
			Mode64 = 1 << 3
		}

		public enum Error {
			OK = 0,
			NoMem,
			Arch,
			Handle,
			Mode,
			Version,
			ReadUnmapped,
			WriteUnmapped,
			FetchUnmapped,
			Hook,
			InsnInvalid,
			Map,
			WriteProt,
			ReadProt,
			FetchProt,
			Arg,
			ReadUnaligned,
			WriteUnaligned,
			FetchUnaligned,
			HookExist,
			Resource,
			Exception
		}

		public enum MemType {
			Read = 16,
			Write,
			Fetch,
			ReadUnmapped,
			WriteUnmapped,
			FetchUnmapped,
			WriteProt,
			ReadProt,
			FetchProt,
			ReadAfter
		}

		[Flags]
		public enum HookType {
			Intr = 1 << 0,
			Insn = 1 << 1,
			Code = 1 << 2,
			Block = 1 << 3,
			MemReadUnmapped = 1 << 4,
			MemWriteUnmapped = 1 << 5,
			MemFetchUnmapped = 1 << 6,
			MemReadProt = 1 << 7,
			MemWriteProt = 1 << 8,
			MemFetchProt = 1 << 9,
			MemRead = 1 << 10,
			MemWrite = 1 << 11,
			MemFetch = 1 << 12,
			MemReadAfter = 1 << 13,
			InsnInvalid = 1 << 14
		}

		public delegate void HookCodeDelegate(ulong address, uint size);
		public delegate void HookIntrDelegate(uint intno);
		public delegate void HookMemDelegate(MemType type, ulong address, int size, long value);
		public delegate bool HookEventMemDelegate(MemType type, ulong address, int size, long value);

		delegate void InternalHookCodeDelegate(IntPtr uc, ulong address, uint size, IntPtr user_data);
		delegate void InternalHookIntrDelegate(IntPtr uc, uint intno, IntPtr user_data);
		delegate void InternalHookInsnInvalidDelegate(IntPtr uc, IntPtr user_data);
		delegate void InternalHookInsnInDelegate(IntPtr uc, uint port, int size, IntPtr user_data);
		delegate void InternalHookInsnOutDelegate(IntPtr uc, uint port, int size, uint value, IntPtr user_data);
		delegate void InternalHookMemDelegate(IntPtr uc, MemType type, ulong address, int size, long value, IntPtr user_data);
		delegate bool InternalHookEventMemDelegate(IntPtr uc, MemType type, ulong address, int size, long value, IntPtr user_data);

		[Flags]
		public enum Prot : uint {
			None = 0,
			Read = 1,
			Write = 2,
			Exec = 4,
			All = 7
		}

		[DllImport("unicorn", EntryPoint = "uc_version")]
		public static extern uint GetVersion(out uint major, out uint minor);

		[DllImport("unicorn")]
		static extern Error uc_open(Arch arch, Mode mode, out IntPtr uc);

		[DllImport("unicorn")]
		static extern Error uc_close(IntPtr uc);

		[DllImport("unicorn", EntryPoint = "uc_reg_write")]
		static extern Error uc_reg_write_u32(IntPtr uc, int regid, ref uint value);

		[DllImport("unicorn", EntryPoint = "uc_reg_read")]
		static extern Error uc_reg_read_u32(IntPtr uc, int regid, out uint value);

		[DllImport("unicorn")]
		static extern Error uc_mem_write(IntPtr uc, ulong address, byte[] bytes, UIntPtr size);

		[DllImport("unicorn")]
		static extern Error uc_mem_read(IntPtr uc, ulong address, [Out] byte[] bytes, UIntPtr size);

		[DllImport("unicorn")]
		static extern Error uc_emu_start(IntPtr uc, ulong begin, ulong until, ulong timeout, UIntPtr count);

		[DllImport("unicorn")]
		static extern Error uc_emu_stop(IntPtr uc);

		[DllImport("unicorn")]
		static extern Error uc_hook_add(IntPtr uc, out UIntPtr hh, HookType type, Delegate callback, IntPtr user_data, ulong begin, ulong end);

		[DllImport("unicorn")]
		static extern Error uc_hook_del(IntPtr uc, UIntPtr hh);

		[DllImport("unicorn")]
		static extern Error uc_mem_map(IntPtr uc, ulong address, UIntPtr size, Prot perms);

		[DllImport("unicorn")]
		static extern Error uc_mem_unmap(IntPtr uc, ulong address, UIntPtr size);

		void ThrowIfNotOK(Error err) {
			if (err != Error.OK) {
				throw new UnicornException(_uc, err);
			}
		}

		private IntPtr _uc = IntPtr.Zero;
		private Dictionary<UIntPtr, Delegate> _hookDelegates = new Dictionary<UIntPtr, Delegate>();

		public Unicorn(Arch arch, Mode mode) {
			ThrowIfNotOK(uc_open(arch, mode, out _uc));
		}

		~Unicorn() {
			Dispose();
		}

		public void Dispose() {
			if (_uc != IntPtr.Zero) {
				uc_close(_uc);
				_uc = IntPtr.Zero;
			}
		}

		public void RegWriteU32(int regid, uint value) {
			ThrowIfNotOK(uc_reg_write_u32(_uc, regid, ref value));
		}

		public uint RegReadU32(int regid) {
			ThrowIfNotOK(uc_reg_read_u32(_uc, regid, out uint value));
			return value;
		}

		public void MemWrite(ulong address, byte[] bytes) {
			ThrowIfNotOK(uc_mem_write(_uc, address, bytes, new UIntPtr((uint) bytes.Length)));
		}

		public byte[] MemRead(ulong address, int size) {
			var bytes = new byte[size];
			ThrowIfNotOK(uc_mem_read(_uc, address, bytes, new UIntPtr((uint) bytes.Length)));
			return bytes;
		}

		public void Start(ulong begin, ulong until, ulong timeout = 0, uint count = 0) {
			ThrowIfNotOK(uc_emu_start(_uc, begin, until, timeout, new UIntPtr(count)));
		}

		public void Stop() {
			ThrowIfNotOK(uc_emu_stop(_uc));
		}

		public UIntPtr HookAddCode(HookType type, HookCodeDelegate callback, ulong begin, ulong end) {
			InternalHookCodeDelegate cb = (uc, address, size, user_data) => callback(address, size);
			ThrowIfNotOK(uc_hook_add(_uc, out UIntPtr hh, type, cb, IntPtr.Zero, begin, end));
			_hookDelegates[hh] = cb;
			return hh;
		}

		public UIntPtr HookAddIntr(HookIntrDelegate callback, ulong begin, ulong end) {
			InternalHookIntrDelegate cb = (uc, intno, user_data) => callback(intno);
			ThrowIfNotOK(uc_hook_add(_uc, out UIntPtr hh, HookType.Intr, cb, IntPtr.Zero, begin, end));
			_hookDelegates[hh] = cb;
			return hh;
		}

		public UIntPtr HookAddMem(HookType type, HookMemDelegate callback, ulong begin, ulong end) {
			InternalHookMemDelegate cb = (uc, type, address, size, value, user_data) => callback(type, address, size, value);
			ThrowIfNotOK(uc_hook_add(_uc, out UIntPtr hh, type, cb, IntPtr.Zero, begin, end));
			_hookDelegates[hh] = cb;
			return hh;
		}

		public UIntPtr HookAddEventMem(HookType type, HookEventMemDelegate callback, ulong begin, ulong end) {
			InternalHookEventMemDelegate cb = (uc, type, address, size, value, user_data) => callback(type, address, size, value);
			ThrowIfNotOK(uc_hook_add(_uc, out UIntPtr hh, type, cb, IntPtr.Zero, begin, end));
			_hookDelegates[hh] = cb;
			return hh;
		}

		public void HookDel(UIntPtr hh) {
			ThrowIfNotOK(uc_hook_del(_uc, hh));
			_hookDelegates.Remove(hh);
		}

		public void MemMap(ulong address, uint size, Prot perms) {
			ThrowIfNotOK(uc_mem_map(_uc, address, new UIntPtr(size), perms));
		}

		public void MemUnmap(ulong address, uint size) {
			ThrowIfNotOK(uc_mem_unmap(_uc, address, new UIntPtr(size)));
		}

		// Utility
		public void WriteU64(ulong address, ulong value) {
			ThrowIfNotOK(uc_mem_write(_uc, address, BitConverter.GetBytes(value), new UIntPtr(8)));
		}

		public void WriteU32(ulong address, uint value) {
			ThrowIfNotOK(uc_mem_write(_uc, address, BitConverter.GetBytes(value), new UIntPtr(4)));
		}

		public void WriteU16(ulong address, ushort value) {
			ThrowIfNotOK(uc_mem_write(_uc, address, BitConverter.GetBytes(value), new UIntPtr(2)));
		}

		public void WriteU8(ulong address, byte value) {
			ThrowIfNotOK(uc_mem_write(_uc, address, BitConverter.GetBytes(value), new UIntPtr(1)));
		}

		public uint ReadU32(ulong address) {
			var bytes = new byte[4];
			ThrowIfNotOK(uc_mem_read(_uc, address, bytes, new UIntPtr(4)));
			return BitConverter.ToUInt32(bytes, 0);
		}

		public ushort ReadU16(ulong address) {
			var bytes = new byte[2];
			ThrowIfNotOK(uc_mem_read(_uc, address, bytes, new UIntPtr(2)));
			return BitConverter.ToUInt16(bytes, 0);
		}

		public String ReadCString(ulong address) {
			if (address == 0)
				return null;

			var buffer = "";
			var bytes = new byte[64];
			for (;;) {
				ThrowIfNotOK(uc_mem_read(_uc, address, bytes, new UIntPtr(64)));
				for (int i = 0; i < 64; i++) {
					if (bytes[i] == 0) {
						buffer += System.Text.Encoding.ASCII.GetString(bytes, 0, i);
						return buffer;
					}
				}
				buffer += System.Text.Encoding.ASCII.GetString(bytes);
				address += 64;
			}
		}

		public uint PopU32() {
			var esp = RegReadU32(REG_ESP);
			var value = ReadU32(esp);
			RegWriteU32(REG_ESP, esp + 4);
			return value;
		}
	}
}
