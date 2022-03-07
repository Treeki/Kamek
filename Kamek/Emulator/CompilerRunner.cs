using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kamek.Emulator {
	public class CompilerRunner {
		struct Import {
			public string DllName;
			public string SymbolName;
			public uint ShimAddress;
			public ShimDelegate Shim;

			public Import WithShim(ShimDelegate s) {
				Shim = s;
				return this;
			}
		}

		bool _debugMode = false;
		uint[] _rollingLog = new uint[128];
		int _rollingLogPosition = 0;

		Unicorn _uc;
		string _exePath;
		uint _imageBase;
		uint _entryPoint;
		uint _rsrcBase;
		// Work region:
		//   - shims
		//   - stack
		uint _workStart;
		uint _workEnd;
		List<Import> _imports = new List<Import>();
		Dictionary<uint, int> _importsByShimAddress = new Dictionary<uint, int>();

		public delegate void ShimDelegate(Unicorn uc);

		public CompilerRunner() {
			_uc = new Unicorn(Unicorn.Arch.X86, Unicorn.Mode.Mode32);
		}

		public void LoadExecutable(string path) {
			// Place an empty page at 0, to cover the FS region
			_uc.MemMap(0, 0x1000, Unicorn.Prot.All);

			// Reserve space for the PE image
			_exePath = path;
			var stream = File.OpenRead(path);
			var pe = new PEFile(stream);
			stream.Dispose();
			_imageBase = pe.Header.OptionalHeader.ImageBase;
			_entryPoint = _imageBase + pe.Header.OptionalHeader.AddressOfEntryPoint;
			_workStart = _imageBase + ((pe.Header.OptionalHeader.SizeOfImage + 0xFFFu) & ~0xFFFu);
			_workEnd = _workStart + 0x8000; // we need a pretty big stack for CW
			_uc.MemMap(_imageBase, _workEnd - _imageBase, Unicorn.Prot.All);

			foreach (var section in pe.Sections) {
				if (section.RawData != null) {
					var addr = _imageBase + section.VirtualAddress;
					if (_debugMode)
						Console.WriteLine($"Section {section.Name} -> {addr:X8}");
					_uc.MemWrite(addr, section.RawData);

					if (section.Name == ".rsrc")
						_rsrcBase = addr;
				}
			}

			// Parse '.idata'
			var idata = _imageBase + pe.Header.OptionalHeader.ImportTableRva;
			var currentShim = _workStart + 0x200; // leave some space for the TIB

			for (;;) {
				var lookupTable = _uc.ReadU32(idata);
				var forwarderChain = _uc.ReadU32(idata + 8);
				var dllNameAddress = _uc.ReadU32(idata + 12);
				var addressTable = _uc.ReadU32(idata + 16);

				if (lookupTable == 0 && forwarderChain == 0 && dllNameAddress == 0 && addressTable == 0)
					break;

				var dllName = _uc.ReadCString(_imageBase + dllNameAddress).ToLower();

				// Go through the IAT
				addressTable += _imageBase;
				for (;;) {
					var lookup = _uc.ReadU32(addressTable);
					if (lookup == 0)
						break;

					string name;
					if ((lookup & 0x80000000u) != 0) {
						name = "func" + (lookup & 0x7FFFFFFFu);
					} else {
						name = _uc.ReadCString(_imageBase + lookup + 2);
					}

					if (_debugMode)
						Console.WriteLine($"  {addressTable:X8} => {dllName}::{name} - Shim: {currentShim:X8}");

					// Generate a shim
					// int 3; retn
					_uc.MemWrite(currentShim, new byte[] { 0xCC, 0xC3 });
					_uc.WriteU32(addressTable, currentShim);
					var index = _imports.Count;
					_imports.Add(new Import { DllName = dllName, SymbolName = name, ShimAddress = currentShim });
					_importsByShimAddress[currentShim] = index;
					currentShim += 2;

					addressTable += 4;
				}

				idata += 20;
			}

			// Set up our hooks
			if (_debugMode) {
				_uc.HookAddCode(Unicorn.HookType.Code, (address, size) => {
					_rollingLog[_rollingLogPosition] = (uint) address;
					_rollingLogPosition = (_rollingLogPosition + 1) % _rollingLog.Length;
				}, 0, 0xFFFFFFFF);
			}

			_uc.HookAddIntr((intno) => {
				var eip = _uc.RegReadU32(Unicorn.REG_EIP);
				var returnAddress = _uc.PopU32();
				//Console.WriteLine($"[e:{eip:X8} r:{returnAddress:X8}]");
				var importIndex = _importsByShimAddress[eip - 1];
				if (_imports[importIndex].Shim == null) {
					Console.WriteLine($"[r:{returnAddress:X8}] UNIMPLEMENTED SHIM {_imports[importIndex].DllName} :: {_imports[importIndex].SymbolName}");
				} else {
					//if (_imports[importIndex].SymbolName == "GetFullPathNameA") Console.WriteLine($"[r:{returnAddress:X8}]");
					_imports[importIndex].Shim(_uc);
				}
				_uc.RegWriteU32(Unicorn.REG_EIP, returnAddress);
			}, 0, 0xFFFFFFFF);

			/*_uc.HookAddMem(Unicorn.HookType.MemRead | Unicorn.HookType.MemReadAfter, (type, address, size, value) => {
				Console.WriteLine($"${type} - a={address:X8} s={size:X8} v={value:X8}");
			}, 0, 0xFFFFFFFF);*/
		}

		public void AddShim(string dll, string name, ShimDelegate shim) {
			for (var i = 0; i < _imports.Count; i++) {
				if (_imports[i].DllName == dll && _imports[i].SymbolName == name) {
					if (_imports[i].Shim != null) {
						throw new InvalidOperationException($"Double shim for {dll}::{name}");
					} else {
						_imports[i] = _imports[i].WithShim(shim);
						return;
					}
				}
			}
			Console.WriteLine($"Unable to find function {dll}::{name} to shim");
		}

		public void Emulate() {
			_uc.RegWriteU32(Unicorn.REG_ESP, _workEnd - 0x10);

			try {
				_uc.Start(_entryPoint, 0, 0, 0);
			} catch (Unicorn.UnicornException ex) {
				if (_debugMode) {
					for (int i = 0; i < _rollingLog.Length; i++) {
						var p = _rollingLog[(i + _rollingLogPosition) % _rollingLog.Length];
						Console.WriteLine("{0:X8}", p);
					}
				}
				Console.WriteLine(ex);
			}
		}

		public void LoadStdLib(IEnumerable<string> args) {
			InitKernel();
			InitHeap();
			InitEnvironment(args);
			InitFiles();
			InitResources();
			InitRegistry();
			InitOLE();
			InitVersionAPI();
			InitMisc();

			// scan for unimplemented functions
			foreach (var import in _imports) {
				if (import.Shim == null && _debugMode) {
					Console.WriteLine($"  Not done: {import.DllName} :: {import.SymbolName}");
				}
			}
		}

#region Kernel
		Dictionary<uint, uint> _tlsValues = new Dictionary<uint, uint>();
		uint _nextTlsValueID = 1;

		uint _lastError = 0;

		void InitKernel() {
			AddShim("kernel32.dll", "GetLastError", uc => {
				uc.RegWriteU32(Unicorn.REG_EAX, _lastError);
			});

			AddShim("kernel32.dll", "GetTickCount", uc => {
				uc.RegWriteU32(Unicorn.REG_EAX, (uint) Environment.TickCount);
			});

			AddShim("kernel32.dll", "GetCurrentProcess", uc => {
				uc.RegWriteU32(Unicorn.REG_EAX, 100);
			});

			AddShim("kernel32.dll", "GetSystemTime", uc => {
				var ptr = uc.PopU32();
				var now = DateTime.UtcNow;
				uc.WriteU16(ptr, (ushort) now.Year);
				uc.WriteU16(ptr + 2, (ushort) now.Month);
				uc.WriteU16(ptr + 4, (ushort) now.DayOfWeek);
				uc.WriteU16(ptr + 6, (ushort) now.Day);
				uc.WriteU16(ptr + 8, (ushort) now.Hour);
				uc.WriteU16(ptr + 10, (ushort) now.Minute);
				uc.WriteU16(ptr + 12, (ushort) now.Second);
				uc.WriteU16(ptr + 14, (ushort) now.Millisecond);
			});

			AddShim("kernel32.dll", "GetLocalTime", uc => {
				var ptr = uc.PopU32();
				var now = DateTime.Now;
				uc.WriteU16(ptr, (ushort) now.Year);
				uc.WriteU16(ptr + 2, (ushort) now.Month);
				uc.WriteU16(ptr + 4, (ushort) now.DayOfWeek);
				uc.WriteU16(ptr + 6, (ushort) now.Day);
				uc.WriteU16(ptr + 8, (ushort) now.Hour);
				uc.WriteU16(ptr + 10, (ushort) now.Minute);
				uc.WriteU16(ptr + 12, (ushort) now.Second);
				uc.WriteU16(ptr + 14, (ushort) now.Millisecond);
			});

			AddShim("kernel32.dll", "SystemTimeToFileTime", uc => {
				var stPtr = uc.PopU32();
				var ftPtr = uc.PopU32();
				var year = uc.ReadU16(stPtr);
				var month = uc.ReadU16(stPtr + 2);
				var day = uc.ReadU16(stPtr + 6);
				var hour = uc.ReadU16(stPtr + 8);
				var minute = uc.ReadU16(stPtr + 10);
				var second = uc.ReadU16(stPtr + 12);
				var millisecond = uc.ReadU16(stPtr + 14);
				var time = new DateTime(year, month, day, hour, minute, second, millisecond, DateTimeKind.Utc);
				uc.WriteU64(ftPtr, (ulong) time.ToFileTimeUtc());
				uc.RegWriteU32(Unicorn.REG_EAX, 1);
			});

			AddShim("kernel32.dll", "InitializeCriticalSection", uc => {
				var p = uc.PopU32();
				//Console.WriteLine($"InitializeCriticalSection({p:X8})");
			});
			AddShim("kernel32.dll", "DeleteCriticalSection", uc => {
				var p = uc.PopU32();
				//Console.WriteLine($"DeleteCriticalSection({p:X8})");
			});
			AddShim("kernel32.dll", "EnterCriticalSection", uc => {
				var p = uc.PopU32();
				//Console.WriteLine($"EnterCriticalSection({p:X8})");
			});
			AddShim("kernel32.dll", "LeaveCriticalSection", uc => {
				var p = uc.PopU32();
				//Console.WriteLine($"LeaveCriticalSection({p:X8})");
			});

			AddShim("kernel32.dll", "TlsAlloc", uc => {
				var id = _nextTlsValueID++;
				_tlsValues[id] = 0;
				uc.RegWriteU32(Unicorn.REG_EAX, id);
			});
			AddShim("kernel32.dll", "TlsFree", uc => {
				var id = uc.PopU32();
				_tlsValues.Remove(id);
				uc.RegWriteU32(Unicorn.REG_EAX, 1);
			});
			AddShim("kernel32.dll", "TlsGetValue", uc => {
				var id = uc.PopU32();
				uc.RegWriteU32(Unicorn.REG_EAX, _tlsValues[id]);
				_lastError = 0;
			});
			AddShim("kernel32.dll", "TlsSetValue", uc => {
				var id = uc.PopU32();
				var value = uc.PopU32();
				_tlsValues[id] = value;
			});

			AddShim("kernel32.dll", "ExitProcess", uc => {
				var exitCode = uc.PopU32();
				// Console.WriteLine($"ExitProcess({exitCode})");
				uc.Stop();
				Environment.Exit((int) exitCode);
			});
		}
#endregion

#region Heap
		private Heap _heap;

		void InitHeap() {
			_heap = new Heap(_uc, 0x1000000, 64 * 1024 * 1024);

			AddShim("kernel32.dll", "GlobalAlloc", uc => {
				var flags = uc.PopU32();
				var bytes = uc.PopU32();
				var handle = 0u;
				if ((flags & 2) != 0) {
					Console.WriteLine("!!!!! cannot allocate moveable handle");
				} else {
					handle = _heap.NewPtr(bytes);
					if (handle == 0) {
						var retn = uc.ReadU32(uc.RegReadU32(Unicorn.REG_ESP));
						Console.WriteLine($" failed to allocate at {retn:X8}");
					}
				}
				uc.RegWriteU32(Unicorn.REG_EAX, handle);
			});

			AddShim("kernel32.dll", "GlobalReAlloc", uc => {
				var handle = uc.PopU32();
				var bytes = uc.PopU32();
				var flags = uc.PopU32();

				var success = _heap.SetPtrSize(handle, bytes);
				if (!success) {
					// move it?
					if ((flags & 2) != 0) {
						var newHandle = _heap.NewPtr(bytes);
						if (newHandle != 0) {
							var oldSize = _heap.GetPtrSize(handle);
							var buffer = uc.MemRead(handle, (int) oldSize);
							uc.MemWrite(newHandle, buffer);
							_heap.DisposePtr(handle);
							handle = newHandle;
							success = true;
						} else {
							Console.WriteLine("!!!!! cannot reallocate moveable handle");
							_lastError = 14; // ERROR_OUTOFMEMORY
						}
					} else {
						Console.WriteLine("!!!!! not allowed to reallocate non-moveable handle");
						_lastError = 212; // ERROR_LOCKED
					}
				}

				uc.RegWriteU32(Unicorn.REG_EAX, success ? handle : 0);
			});

			AddShim("kernel32.dll", "GlobalFree", uc => {
				var handle = uc.PopU32();
				_heap.DisposePtr(handle);
				uc.RegWriteU32(Unicorn.REG_EAX, 0);
			});

			AddShim("kernel32.dll", "GlobalFlags", uc => {
				var handle = uc.PopU32();
				uc.RegWriteU32(Unicorn.REG_EAX, 0);
			});
		}
#endregion

#region Environment
		private uint _commandLinePointer;

		void InitEnvironment(IEnumerable<string> args) {
			// Build our string
			var buffer = new StringBuilder();
			buffer.Append("B:\\mwcceppc");
			foreach (var arg in args) {
				var munged = ToWindowsPath(arg);
				buffer.Append(' ');
				buffer.Append('"');
				if (munged.Contains('"')) {
					foreach (var ch in munged) {
						buffer.Append(ch);
						if (ch == '"')
							buffer.Append('"'); // two of them
					}
				} else {
					buffer.Append(munged);
				}
				buffer.Append('"');
			}

			var commandLineBuffer = Encoding.UTF8.GetBytes(buffer.ToString());
			_commandLinePointer = _heap.NewPtr((uint) commandLineBuffer.Length + 1);
			_uc.MemWrite(_commandLinePointer, commandLineBuffer);

			AddShim("kernel32.dll", "GetCommandLineA", uc => {
				uc.RegWriteU32(Unicorn.REG_EAX, _commandLinePointer);
			});

			AddShim("kernel32.dll", "GetEnvironmentStrings", uc => {
				var buffer = BuildEnvironment();
				var pointer = _heap.NewPtr((uint) buffer.Length);
				uc.MemWrite(pointer, buffer);
				uc.RegWriteU32(Unicorn.REG_EAX, pointer);
			});

			AddShim("kernel32.dll", "FreeEnvironmentStringsA", uc => {
				var pointer = uc.PopU32();
				_heap.DisposePtr(pointer);
				uc.RegWriteU32(Unicorn.REG_EAX, 1);
			});
		}

		byte[] BuildEnvironment() {
			var sb = new StringBuilder();
			foreach (DictionaryEntry pair in Environment.GetEnvironmentVariables()) {
				string key = pair.Key as string;
				string value = pair.Value as string;
				if (key != null && value != null) {
					sb.Append(key);
					sb.Append('=');
					sb.Append(value);
					sb.Append('\0');
				}
			}
			sb.Append('\0');
			return Encoding.UTF8.GetBytes(sb.ToString());
		}
#endregion

#region Files
		Dictionary<uint, Stream> _deviceHandles = new Dictionary<uint, Stream>();
		Dictionary<uint, byte[]> _fileMappingHandles = new Dictionary<uint, byte[]>();
		uint _nextDeviceHandle = 1;
		uint _stdinHandle;
		uint _stdoutHandle;
		uint _stderrHandle;

		static string FromWindowsPath(string winPath) {
			if (Path.DirectorySeparatorChar == '\\') {
				return winPath;
			} else {
				if (winPath.StartsWith("Z:", true, null))
					winPath = winPath.Substring(2);
				return winPath.Replace('\\', Path.DirectorySeparatorChar);
			}
		}
		static string ToWindowsPath(string nativePath) {
			if (Path.DirectorySeparatorChar == '\\') {
				return nativePath;
			} else {
				if (Path.IsPathRooted(nativePath))
					return "Z:" + nativePath.Replace(Path.DirectorySeparatorChar, '\\');
				else
					return nativePath.Replace(Path.DirectorySeparatorChar, '\\');
			}
		}

		void InitFiles() {
			_stdinHandle = _nextDeviceHandle++;
			_stdoutHandle = _nextDeviceHandle++;
			_stderrHandle = _nextDeviceHandle++;
			_deviceHandles[_stdinHandle] = Console.OpenStandardInput();
			_deviceHandles[_stdoutHandle] = Console.OpenStandardOutput();
			_deviceHandles[_stderrHandle] = Console.OpenStandardError();

			AddShim("kernel32.dll", "CreateFileA", uc => {
				var filename = uc.ReadCString(uc.PopU32());
				var desiredAccess = uc.PopU32();
				var shareMode = uc.PopU32();
				var securityAttributes = uc.PopU32();
				var creationDisposition = uc.PopU32();
				var flagsAndAttributes = uc.PopU32();
				var templateFile = uc.PopU32();

				if (_debugMode)
					Console.WriteLine($"CreateFileA(\"{filename}\", desiredAccess={desiredAccess}, shareMode={shareMode}, securityAttributes={securityAttributes}, creationDisposition={creationDisposition}, flagsAndAttributes={flagsAndAttributes}, templateFile={templateFile}");

				if (filename == "B:\\mwcceppc.exe")
					filename = _exePath;
				else
					filename = FromWindowsPath(filename);

				var mode = (FileMode) creationDisposition;
				var access = (FileAccess) 0;
				if ((desiredAccess & 0x80000000u) != 0)
					access |= FileAccess.Read;
				if ((desiredAccess & 0x40000000u) != 0)
					access |= FileAccess.Write;
				var share = (FileShare) (shareMode & 7);

				_lastError = 0;
				if (creationDisposition == 4 || creationDisposition == 2) {
					if (File.Exists(filename))
						_lastError = 183; // ERROR_ALREADY_EXISTS
				}

				var handle = 0u;

				try {
					var file = File.Open(filename, mode, access, share);
					handle = _nextDeviceHandle++;
					_deviceHandles[handle] = file;
					// Console.WriteLine($"created file with handle {handle}");
				} catch (FileNotFoundException ex) {
					Console.WriteLine($"CreateFile error: {ex}");
					if (creationDisposition == 3 || creationDisposition == 5)
						_lastError = 2; // ERROR_FILE_NOT_FOUND
					else
						_lastError = 110; // ERROR_OPEN_FAILED
				} catch (IOException ex) {
					Console.WriteLine($"CreateFile error: {ex}");
					if (creationDisposition == 1)
						_lastError = 80; // ERROR_FILE_EXISTS
					else
						_lastError = 110; // ERROR_OPEN_FAILED
				}

				uc.RegWriteU32(Unicorn.REG_EAX, handle);
			});

			AddShim("kernel32.dll", "ReadFile", uc => {
				var handle = uc.PopU32();
				var bufferPtr = uc.PopU32();
				var amount = uc.PopU32();
				var amountPtr = uc.PopU32();
				var overlapped = uc.PopU32();

				var stream = _deviceHandles[handle];
				var buffer = new byte[amount];
				var result = 0u;

				try {
					var actualAmount = stream.Read(buffer, 0, buffer.Length);
					uc.MemWrite(bufferPtr, buffer);
					if (amountPtr != 0)
						uc.WriteU32(amountPtr, (uint) actualAmount);
					result = 1;
				} catch (Exception e) {
					Console.WriteLine($"Read failed: {e}");
					_lastError = 30; // ERROR_READ_FAULT
				}

				uc.RegWriteU32(Unicorn.REG_EAX, result);
			});

			AddShim("kernel32.dll", "WriteFile", uc => {
				var handle = uc.PopU32();
				var bufferPtr = uc.PopU32();
				var amount = uc.PopU32();
				var amountPtr = uc.PopU32();
				var overlapped = uc.PopU32();

				// Console.WriteLine($"WriteFile(handle={handle}, buffer={bufferPtr:X8}, amount={amount}, amountPtr={amountPtr:X8})");
				var stream = _deviceHandles[handle];
				var buffer = uc.MemRead(bufferPtr, (int) amount);
				var result = 0u;

				try {
					stream.Write(buffer, 0, buffer.Length);
					if (amountPtr != 0)
						uc.WriteU32(amountPtr, amount);
					result = 1;
				} catch (Exception e) {
					Console.WriteLine($"Write failed: {e}");
					_lastError = 29; // ERROR_WRITE_FAULT
				}

				uc.RegWriteU32(Unicorn.REG_EAX, result);
			});

			AddShim("kernel32.dll", "GetFileSize", uc => {
				var handle = uc.PopU32();
				var highPtr = uc.PopU32();

				if (_deviceHandles.ContainsKey(handle)) {
					var stream = _deviceHandles[handle];
					var size = stream.Length;
					if (highPtr != 0)
						uc.WriteU32(highPtr, (uint) (size >> 32));
					uc.RegWriteU32(Unicorn.REG_EAX, (uint) (size & 0xFFFFFFFF));
					var file = stream as FileStream;
					_lastError = 0;
				} else {
					uc.RegWriteU32(Unicorn.REG_EAX, 0xFFFFFFFFu);
					_lastError = 6; // ERROR_INVALID_HANDLE
				}
			});

			AddShim("kernel32.dll", "SetFilePointer", uc => {
				var handle = uc.PopU32();
				var distanceLow = uc.PopU32();
				var distanceHighPtr = uc.PopU32();
				var moveMethod = uc.PopU32();

				var uDistance = (ulong) distanceLow;
				if (distanceHighPtr != 0) {
					var distanceHigh = (ulong) uc.ReadU32(distanceHighPtr);
					uDistance |= (distanceHigh << 32);
				}
				var distance = (long) uDistance;

				var file = _deviceHandles[handle];
				try {
					_lastError = 0;
					var newPos = file.Seek(distance, (SeekOrigin) moveMethod);
					if (distanceHighPtr != 0) {
						uc.WriteU32(distanceHighPtr, (uint) (newPos >> 32));
					}
					uc.RegWriteU32(Unicorn.REG_EAX, (uint) (newPos & 0xFFFFFFFF));
				} catch (IOException e) {
					Console.WriteLine($"Seek failed: {e}");
					_lastError = 132; // ERROR_SEEK_ON_DEVICE is probably it
					uc.RegWriteU32(Unicorn.REG_EAX, 0xFFFFFFFFu);
				}
			});

			AddShim("kernel32.dll", "DeleteFileA", uc => {
				var path = uc.ReadCString(uc.PopU32());
				var native = FromWindowsPath(path);
				
				if (_debugMode)
					Console.WriteLine($"CURRENTLY IGNORED -- DeleteFileA(\"{path}\" / \"{native}\")");

				if (File.Exists(native)) {
					// pretend we did it, for now
					uc.RegWriteU32(Unicorn.REG_EAX, 1);
				} else {
					_lastError = 2; // ERROR_FILE_NOT_FOUND
					uc.RegWriteU32(Unicorn.REG_EAX, 0);
				}
			});

			AddShim("kernel32.dll", "CreateFileMappingA", uc => {
				var fileHandle = uc.PopU32();
				var fileMappingAttributes = uc.PopU32();
				var protect = uc.PopU32();
				var maxSizeHigh = uc.PopU32();
				var maxSizeLow = uc.PopU32();
				var namePtr = uc.PopU32();

				if (_deviceHandles.ContainsKey(fileHandle)) {
					if ((protect & 4) != 0) {
						Console.WriteLine("our CreateFileMappingA does not support writable mappings");
					}

					// just read the whole file in
					var file = _deviceHandles[fileHandle];
					var savePos = file.Position;
					file.Position = 0;
					var buffer = new byte[file.Length];
					file.Read(buffer, 0, buffer.Length);
					file.Position = savePos;

					var mappingHandle = _nextDeviceHandle++;
					_fileMappingHandles[mappingHandle] = buffer;
					uc.RegWriteU32(Unicorn.REG_EAX, mappingHandle);
				} else {
					uc.RegWriteU32(Unicorn.REG_EAX, 0);
					_lastError = 6; // ERROR_INVALID_HANDLE
				}
			});

			AddShim("kernel32.dll", "MapViewOfFile", uc => {
				var mappingHandle = uc.PopU32();
				var desiredAccess = uc.PopU32();
				var offsetHigh = uc.PopU32();
				var offsetLow = uc.PopU32();
				var numberOfBytes = uc.PopU32();

				// we cheese this entirely by just giving them a buffer
				var fileBuffer = _fileMappingHandles[mappingHandle];
				var tempBuffer = new byte[(int) numberOfBytes];
				Array.Copy(fileBuffer, offsetLow, tempBuffer, 0, numberOfBytes);

				var ptr = _heap.NewPtr(numberOfBytes);
				uc.MemWrite(ptr, tempBuffer);

				uc.RegWriteU32(Unicorn.REG_EAX, ptr);
			});

			AddShim("kernel32.dll", "UnmapViewOfFile", uc => {
				var ptr = uc.PopU32();
				_heap.DisposePtr(ptr);
				uc.RegWriteU32(Unicorn.REG_EAX, 1);
			});

			AddShim("kernel32.dll", "CloseHandle", uc => {
				var handle = uc.PopU32();
				// Console.WriteLine($"closing handle {handle}");
				if (_deviceHandles.ContainsKey(handle)) {
					_deviceHandles[handle].Dispose();
					_deviceHandles.Remove(handle);
				} else if (_fileMappingHandles.ContainsKey(handle)) {
					_fileMappingHandles.Remove(handle);
				}
				uc.RegWriteU32(Unicorn.REG_EAX, 1);
			});

			AddShim("kernel32.dll", "DuplicateHandle", uc => {
				var sourceProcessHandle = uc.PopU32();
				var sourceHandle = uc.PopU32();
				var targetProcessHandle = uc.PopU32();
				var targetHandlePtr = uc.PopU32();
				var desiredAccess = uc.PopU32();
				var inheritHandle = uc.PopU32();
				var options = uc.PopU32();

				var newHandle = _nextDeviceHandle++;
				_deviceHandles[newHandle] = _deviceHandles[sourceHandle];
				uc.WriteU32(targetHandlePtr, newHandle);
				uc.RegWriteU32(Unicorn.REG_EAX, 1);
			});

			AddShim("kernel32.dll", "GetStdHandle", uc => {
				var id = uc.PopU32();
				if (id == 0xFFFFFFF6)
					uc.RegWriteU32(Unicorn.REG_EAX, _stdinHandle);
				else if (id == 0xFFFFFFF5)
					uc.RegWriteU32(Unicorn.REG_EAX, _stdoutHandle);
				else if (id == 0xFFFFFFF4)
					uc.RegWriteU32(Unicorn.REG_EAX, _stderrHandle);
				else
					uc.RegWriteU32(Unicorn.REG_EAX, 0xFFFFFFFFu);
			});

			AddShim("kernel32.dll", "GetFullPathNameA", uc => {
				var filename = uc.ReadCString(uc.PopU32());
				var native = FromWindowsPath(filename);
				var bufferLength = uc.PopU32();
				var bufferPtr = uc.PopU32();
				var filePartPtr = uc.PopU32();
				// Console.WriteLine($"GetFullPathNameA(filename=\"{filename}\" / \"{native}\", bufferLength={bufferLength}, bufferPtr={bufferPtr:X8}, filePartPtr={filePartPtr:X8})");

				var fullPath = "";

				// KLUDGE
				if (filename.StartsWith("B:\\")) {
					fullPath = filename;
				} else if (filename.StartsWith("B:")) {
					fullPath = "B:\\" + filename.Substring(2);
				} else {
					fullPath = ToWindowsPath(Path.GetFullPath(native));
				}

				// Console.WriteLine($" ... -> \"{fullPath}\"");

				var fullPathBytes = Encoding.UTF8.GetBytes(fullPath);
				uc.MemWrite(bufferPtr, fullPathBytes);
				uc.WriteU8((ulong) (bufferPtr + fullPathBytes.Length), 0);
				if (filePartPtr != 0) {
					uc.WriteU32(filePartPtr, 0);
					for (var i = fullPathBytes.Length - 2; i >= 2; --i) {
						if (fullPathBytes[i] == '\\') {
							uc.WriteU32(filePartPtr, bufferPtr + (uint) i + 1);
							break;
						}
					}
				}
				uc.RegWriteU32(Unicorn.REG_EAX, (uint) fullPathBytes.Length);
			});

			AddShim("kernel32.dll", "GetFileTime", uc => {
				var handle = uc.PopU32();
				var creationTimePtr = uc.PopU32();
				var lastAccessTimePtr = uc.PopU32();
				var lastWriteTimePtr = uc.PopU32();

				var file = _deviceHandles[handle] as FileStream;
				var info = new FileInfo(file.Name);
				if (creationTimePtr != 0)
					uc.WriteU64(creationTimePtr, (ulong) info.CreationTime.ToFileTime());
				if (lastAccessTimePtr != 0)
					uc.WriteU64(lastAccessTimePtr, (ulong) info.LastAccessTime.ToFileTime());
				if (lastWriteTimePtr != 0)
					uc.WriteU64(lastWriteTimePtr, (ulong) info.LastWriteTime.ToFileTime());

				uc.RegWriteU32(Unicorn.REG_EAX, 1);
			});

			AddShim("kernel32.dll", "GetFileAttributesA", uc => {
				var filename = uc.ReadCString(uc.PopU32());
				var native = FromWindowsPath(filename);
				// Console.WriteLine($"GetFileAttributesA(\"{filename}\" / \"{native}\")");

				var result = 0xFFFFFFFFu;

				// KLUDGE
				if (filename == "B:\\mwcceppc.exe") {
					result = 0x80; // FILE_ATTRIBUTE_NORMAL
				} else if (!filename.StartsWith("B:\\")) {
					// normal filesystem query
					try {
						var attrs = File.GetAttributes(native);
						result = (uint) attrs;
					} catch (Exception) {
						// nah.
						_lastError = 2; // ERROR_FILE_NOT_FOUND
					}
				} else {
					// we do not perceive this
					_lastError = 2; // ERROR_FILE_NOT_FOUND
				}

				// Console.WriteLine($"returning {result:X8}");
				uc.RegWriteU32(Unicorn.REG_EAX, result);
			});

			AddShim("kernel32.dll", "FindFirstFileA", uc => {
				var filename = uc.ReadCString(uc.PopU32());
				var dataPtr = uc.PopU32();

				// Console.WriteLine($"FindFirstFileA(\"{filename}\", {dataPtr:X8})");

				// split it up
				var path = ".";
				var sepIndex = filename.LastIndexOf('\\');
				if (sepIndex != -1) {
					path = filename.Substring(0, sepIndex);
					filename = filename.Substring(sepIndex + 1);
				}

				path = FromWindowsPath(path);

				var enumerator = Directory.EnumerateFileSystemEntries(path, filename).GetEnumerator();
				if (enumerator.MoveNext()) {
					FillFindDataStructure(dataPtr, enumerator.Current);

					var handle = _nextFindDataHandle++;
					_findData[handle] = enumerator;
					uc.RegWriteU32(Unicorn.REG_EAX, handle);
				} else {
					_lastError = 2; // ERROR_FILE_NOT_FOUND
					uc.RegWriteU32(Unicorn.REG_EAX, 0xFFFFFFFFu);
				}
			});

			AddShim("kernel32.dll", "FindNextFileA", uc => {
				var handle = uc.PopU32();
				var dataPtr = uc.PopU32();

				var enumerator = _findData[handle];
				if (enumerator.MoveNext()) {
					FillFindDataStructure(dataPtr, enumerator.Current);
					uc.RegWriteU32(Unicorn.REG_EAX, 1);
				} else {
					_lastError = 18; // ERROR_NO_MORE_FILES
					uc.RegWriteU32(Unicorn.REG_EAX, 0);
				}
			});

			AddShim("kernel32.dll", "FindClose", uc => {
				var handle = uc.PopU32();

				if (_findData.ContainsKey(handle)) {
					_findData.Remove(handle);
					uc.RegWriteU32(Unicorn.REG_EAX, 1);
				} else {
					_lastError = 6; // ERROR_INVALID_HANDLE
					uc.RegWriteU32(Unicorn.REG_EAX, 0);
				}
			});

			AddShim("kernel32.dll", "SetConsoleCtrlHandler", uc => {
				var handler = uc.PopU32();
				var add = uc.PopU32();
				// Console.WriteLine($"SetConsoleCtrlHandler(handler={handler:X8}, add={add})");
				uc.RegWriteU32(Unicorn.REG_EAX, 1); // pretend we succeeded
			});

			AddShim("kernel32.dll", "GetConsoleScreenBufferInfo", uc => {
				var outputHandle = uc.PopU32();
				var ptr = uc.PopU32();
				// dwSize
				uc.WriteU16(ptr, (ushort) Console.BufferWidth);
				uc.WriteU16(ptr + 2, (ushort) Console.BufferHeight);
				// dwCursorPosition
				uc.WriteU16(ptr + 4, (ushort) Console.CursorLeft);
				uc.WriteU16(ptr + 6, (ushort) Console.CursorTop);
				// wAttributes (we ignore it lol)
				uc.WriteU16(ptr + 8, 0);
				// srWindow
				uc.WriteU16(ptr + 10, (ushort) Console.WindowLeft);
				uc.WriteU16(ptr + 12, (ushort) Console.WindowTop);
				uc.WriteU16(ptr + 14, (ushort) (Console.WindowLeft + Console.WindowWidth));
				uc.WriteU16(ptr + 16, (ushort) (Console.WindowTop + Console.WindowHeight));
				// dwMaximumWindowSize
				uc.WriteU16(ptr + 18, (ushort) Console.LargestWindowWidth);
				uc.WriteU16(ptr + 20, (ushort) Console.LargestWindowHeight);

				uc.RegWriteU32(Unicorn.REG_EAX, 1);
			});

			AddShim("kernel32.dll", "GetSystemDirectoryA", uc => {
				var buffer = uc.PopU32();
				var maxSize = uc.PopU32();
				uc.RegWriteU32(Unicorn.REG_EAX, 0); // fail
			});

			AddShim("kernel32.dll", "GetWindowsDirectoryA", uc => {
				var buffer = uc.PopU32();
				var maxSize = uc.PopU32();
				uc.RegWriteU32(Unicorn.REG_EAX, 0); // fail
			});

			AddShim("kernel32.dll", "GetCurrentDirectoryA", uc => {
				var maxSize = uc.PopU32();
				var buffer = uc.PopU32();
				var directory = ToWindowsPath(Environment.CurrentDirectory);
				// Console.WriteLine($"GetCurrentDirectoryA({buffer:X8}, {maxSize}) -> \"{directory}\"");
				var dirBytes = Encoding.UTF8.GetBytes(directory);
				uc.MemWrite(buffer, dirBytes);
				uc.WriteU8(buffer + (uint) dirBytes.Length, 0);
				uc.RegWriteU32(Unicorn.REG_EAX, (uint) dirBytes.Length);
			});
		}

		void FillFindDataStructure(uint ptr, string path) {
			var winPath = ToWindowsPath(path);
			var attributes = File.GetAttributes(path);
			FileSystemInfo info;

			if (attributes.HasFlag(FileAttributes.Directory)) {
				info = new DirectoryInfo(path);
			} else {
				var fileInfo = new FileInfo(path);
				info = fileInfo;

				_uc.WriteU32(ptr + 0x1C, (uint) (fileInfo.Length >> 32));
				_uc.WriteU32(ptr + 0x20, (uint) (fileInfo.Length & 0xFFFFFFFFu));
			}

			_uc.WriteU32(ptr, (uint) info.Attributes);

			_uc.WriteU64(ptr + 4, (ulong) info.CreationTime.ToFileTime());
			_uc.WriteU64(ptr + 0xC, (ulong) info.LastAccessTime.ToFileTime());
			_uc.WriteU64(ptr + 0x14, (ulong) info.LastWriteTime.ToFileTime());

			var nameBuffer = Encoding.UTF8.GetBytes(info.Name);
			_uc.MemWrite(ptr + 0x2C, nameBuffer);
			_uc.WriteU8(ptr + 0x2C + (ulong) nameBuffer.Length, 0);
		}

		Dictionary<uint, IEnumerator<string>> _findData = new Dictionary<uint, IEnumerator<string>>();
		uint _nextFindDataHandle = 1;
#endregion

#region Resources
		void InitResources() {
			AddShim("kernel32.dll", "GetModuleHandleA", uc => {
				var name = uc.ReadCString(uc.PopU32());
				// Console.WriteLine($"GetModuleHandleA(\"{name}\")");
				uc.RegWriteU32(Unicorn.REG_EAX, 0x400000);
			});

			AddShim("kernel32.dll", "GetModuleFileNameA", uc => {
				var module = uc.PopU32();
				var filenamePtr = uc.PopU32();
				var bufferSize = uc.PopU32();
				// Console.WriteLine($"GetModuleFileNameA({module:X8})");
				if (module == 0 || module == 0x400000) {
					var name = Encoding.UTF8.GetBytes("B:\\mwcceppc.exe");
					uc.MemWrite(filenamePtr, name);
					uc.RegWriteU32(Unicorn.REG_EAX, (uint) name.Length - 1);
				} else {
					_lastError = 126; // ERROR_MOD_NOT_FOUND
					uc.RegWriteU32(Unicorn.REG_EAX, 0);
				}
			});

			AddShim("kernel32.dll", "LoadLibraryA", uc => {
				var name = uc.ReadCString(uc.PopU32());
				// Console.WriteLine($"LoadLibraryA(\"{name}\")");
				if (name == "B:\\mwcceppc.exe") {
					// already loaded, or so the app thinks
					uc.RegWriteU32(Unicorn.REG_EAX, 0x400000);
				} else {
					_lastError = 126; // ERROR_MOD_NOT_FOUND
					uc.RegWriteU32(Unicorn.REG_EAX, 0);
				}
			});

			AddShim("kernel32.dll", "FreeLibrary", uc => {
				var module = uc.PopU32();
				// Console.WriteLine($"FreeLibrary({module:X8})");
				uc.RegWriteU32(Unicorn.REG_EAX, 1); // tell them it succeeded
			});

			AddShim("user32.dll", "LoadStringA", uc => {
				var module = uc.PopU32();
				var id = uc.PopU32();
				var bufferPtr = uc.PopU32();
				var bufferMax = uc.PopU32();
				// Console.WriteLine($"LoadStringA({module:X8}, id={id})");
				var str = GetStringFromTable(id);
				if (str != 0) {
					var strLength = (uint) uc.ReadU16(str);
					if (bufferMax == 0) {
						uc.WriteU32(bufferPtr, str + 2);
					} else {
						if (strLength >= (bufferMax - 1))
							strLength = bufferMax - 1;
						for (uint i = 0; i < strLength; i++) {
							var ch = uc.ReadU16(str + 2 + i * 2);
							uc.WriteU8(bufferPtr + i, (byte) ch);
						}
						uc.WriteU8(bufferPtr + strLength, 0);
					}
					uc.RegWriteU32(Unicorn.REG_EAX, strLength);
				} else {
					uc.RegWriteU32(Unicorn.REG_EAX, 0);
				}
			});

			AddShim("kernel32.dll", "FindResourceA", uc => {
				var module = uc.PopU32();
				var namePtr =  uc.PopU32();
				var typePtr = uc.PopU32();
				var name = (namePtr < 0x10000) ? ("#" + namePtr) : uc.ReadCString(namePtr);
				var type = (typePtr < 0x10000) ? ("#" + typePtr) : uc.ReadCString(typePtr);
				// Console.WriteLine($"FindResourceA(module={module:X8}, name={name}, type={type})");

				_lastError = 1813; // ERROR_RESOURCE_TYPE_NOT_FOUND
				uc.RegWriteU32(Unicorn.REG_EAX, 0);
			});
		}

		uint SearchResourceTableByID(uint tableAddr, uint id) {
			// Parse table header
			var nameEntryCount = _uc.ReadU16(tableAddr + 12);
			var idEntryCount = _uc.ReadU16(tableAddr + 14);
			tableAddr += 16;
			tableAddr += ((uint) nameEntryCount * 8);

			for (var i = 0; i < idEntryCount; i++) {
				var checkId = _uc.ReadU32(tableAddr);
				if (id == checkId) {
					return _uc.ReadU32(tableAddr + 4);
				}
				tableAddr += 8;
			}

			return 0;
		}

		uint GetResourceByID(uint typeID, uint nameID, uint languageID) {
			if (_rsrcBase == 0) {
				_lastError = 1812; // ERROR_RESOURCE_DATA_NOT_FOUND
				return 0;
			}

			var typeTable = SearchResourceTableByID(_rsrcBase, typeID) & 0x7FFFFFFFu;
			if (typeTable == 0) {
				_lastError = 1813; // ERROR_RESOURCE_TYPE_NOT_FOUND
				return 0;
			}

			var nameTable = SearchResourceTableByID(_rsrcBase + typeTable, nameID) & 0x7FFFFFFFu;
			if (nameTable == 0) {
				_lastError = 1814; // ERROR_RESOURCE_NAME_NOT_FOUND
				return 0;
			}

			var langEntry = SearchResourceTableByID(_rsrcBase + nameTable, languageID);
			if (langEntry == 0) {
				_lastError = 1814; // ERROR_RESOURCE_NAME_NOT_FOUND
				return 0;
			}

			return _rsrcBase + langEntry;
		}

		uint GetStringFromTable(uint stringID) {
			var tableID = (stringID >> 4) + 1;
			var entryID = stringID & 15;
			var stringTable = GetResourceByID(6, tableID, 1033);
			if (stringTable == 0)
				return 0;

			// what's in here?
			var str = _imageBase + _uc.ReadU32(stringTable);
			var size = _uc.ReadU32(stringTable + 4);

			// skip over strings to get to the one we want
			for (var i = 0; i < entryID; i++) {
				var stringSize = _uc.ReadU16(str);
				str += 2;
				str += (uint) stringSize * 2;
			}

			return str;
		}
#endregion

#region Registry
		void InitRegistry() {
			AddShim("advapi32.dll", "RegOpenKeyExA", uc => {
				var key = uc.PopU32();
				var subKey = uc.ReadCString(uc.PopU32());
				var options = uc.PopU32();
				var sam = uc.PopU32();
				var resultPtr = uc.PopU32();

				// Console.WriteLine($"RegOpenKeyEx(key={key:X8}, subKey=\"{subKey}\", options={options}, sam={sam:X8}, resultPtr={resultPtr:X8})");

				uc.RegWriteU32(Unicorn.REG_EAX, 1);
			});
		}
#endregion

#region OLE
		void InitOLE() {
			AddShim("ole32.dll", "CoInitialize", uc => {
				var reserved = uc.PopU32();
				// Console.WriteLine($"CoInitialized(lpReserved={reserved:X8})");
			});

			AddShim("ole32.dll", "CoCreateInstance", uc => {
				var clsidPtr = uc.PopU32();
				var pUnkOuter = uc.PopU32();
				var clsContext = uc.PopU32();
				var iidPtr = uc.PopU32();
				var outPtr = uc.PopU32();

				var clsid0 = uc.ReadU32(clsidPtr);
				var clsid1 = uc.ReadU16(clsidPtr + 4);
				var clsid2 = uc.ReadU16(clsidPtr + 6);
				// these last two should be byteswapped, but w/e
				var clsid3a = uc.ReadU32(clsidPtr + 8);
				var clsid3b = uc.ReadU32(clsidPtr + 12);
				var clsid = $"{{ {clsid0:X8}-{clsid1:X4}-{clsid2:X4}-{clsid3a:X8}{clsid3b:X8} }}";

				var iid0 = uc.ReadU32(iidPtr);
				var iid1 = uc.ReadU16(iidPtr + 4);
				var iid2 = uc.ReadU16(iidPtr + 6);
				var iid3a = uc.ReadU32(iidPtr + 8);
				var iid3b = uc.ReadU32(iidPtr + 12);
				var iid = $"{{ {iid0:X8}-{iid1:X4}-{iid2:X4}-{iid3a:X8}{iid3b:X8} }}";

				// Console.WriteLine($"CoCreateInstance({clsid}, {pUnkOuter:X8}, {clsContext:X8}, {iid}, {outPtr:X8})");

				uc.RegWriteU32(Unicorn.REG_EAX, 0x80040154u); // REGDB_E_CLASSNOTREG
			});
		}
#endregion

#region Version API
		void InitVersionAPI() {
			AddShim("version.dll", "GetFileVersionInfoSizeA", uc => {
				var filename = uc.ReadCString(uc.PopU32());
				var handlePtr = uc.PopU32();
				// Console.WriteLine($"GetFileVersionInfoSizeA(\"{filename}\")");

				uc.RegWriteU32(Unicorn.REG_EAX, 0); // fail for now
			});
		}
#endregion

#region Misc
		void InitMisc() {
			AddShim("lmgr11.dll", "func190", uc => {
				var esp = uc.RegReadU32(Unicorn.REG_ESP);
				var num1 = uc.ReadU32(esp);
				var num2 = uc.ReadU32(esp + 4);
				var str1 = uc.ReadCString(uc.ReadU32(esp + 8));
				var str2 = uc.ReadCString(uc.ReadU32(esp + 12));
				var num3 = uc.ReadU32(esp + 16);
				var str3 = uc.ReadCString(uc.ReadU32(esp + 20));
				var resultPtr = uc.ReadU32(esp + 24);
				// Console.WriteLine($"lp_checkout({num1:X8}, {num2:X8}, \"{str1}\", \"{str2}\", {num3}, \"{str3}\", {resultPtr:X8})");

				uc.RegWriteU32(Unicorn.REG_EAX, 0);
				uc.WriteU32(resultPtr, 0x1234);
			});

			AddShim("lmgr11.dll", "func189", uc => {
				var esp = uc.RegReadU32(Unicorn.REG_ESP);
				var num = uc.ReadU32(esp);
				// Console.WriteLine($"lp_checkin({num:X8})");
				uc.RegWriteU32(Unicorn.REG_EAX, 0);
			});

			AddShim("user32.dll", "MessageBoxA", uc => {
				var hwnd = uc.PopU32();
				var textPtr = uc.PopU32();
				var captionPtr = uc.PopU32();
				var text = uc.ReadCString(textPtr);
				var caption = uc.ReadCString(captionPtr);
				var flags = uc.PopU32();
				Console.WriteLine($"--- [{caption}] ---");
				Console.WriteLine(text);
				uc.RegWriteU32(Unicorn.REG_EAX, 2); //IDCANCEL
			});
		}
#endregion
	}
}
