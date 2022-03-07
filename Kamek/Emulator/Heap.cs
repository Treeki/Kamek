using System;

namespace Kamek.Emulator {
	class Heap {
		readonly Unicorn _uc;

		uint _firstBlock;
		uint _lastBlock;

		const int HDR_USER_SIZE = 0;
		const int HDR_BLOCK_SIZE = 4;
		const int HDR_PREV = 8;
		const int HDR_NEXT = 12;
		const int SIZE_OF_HEADER = 16;
		const uint FREE_FLAG = 0x80000000u;

		public Heap(Unicorn uc, uint arenaStart, uint arenaSize) {
			_uc = uc;

			_uc.MemMap(arenaStart, arenaSize, Unicorn.Prot.Read | Unicorn.Prot.Write);

			// create one large block, covering the entire arena
			var block = arenaStart;
			_uc.WriteU32(block + HDR_USER_SIZE, FREE_FLAG);
			_uc.WriteU32(block + HDR_BLOCK_SIZE, arenaSize);
			_uc.WriteU32(block + HDR_PREV, 0);
			_uc.WriteU32(block + HDR_NEXT, 0);

			_firstBlock = block;
			_lastBlock = block;
		}

		public uint NewPtr(uint size) {
			uint alignedSize = (size + 0xFu) & ~0xFu;
			var block = FindFreeBlock(alignedSize);

			if (block.HasValue) {
				var ptr = block.Value + SIZE_OF_HEADER;
				_uc.WriteU32(block.Value + HDR_USER_SIZE, size);
				ShrinkUsedBlockBySplitting(block.Value);

				var zeroes = new byte[] { 0 };
				for (var i = 0u; i < size; i++) {
					_uc.MemWrite(ptr + i, zeroes);
				}
				return ptr;
			} else {
				Console.WriteLine($"Failed to allocate {size} bytes!");
				return 0;
			}
		}

		public void DisposePtr(uint ptr) {
			var block = ptr - SIZE_OF_HEADER;
			var prev = _uc.ReadU32(block + HDR_PREV);
			var next = _uc.ReadU32(block + HDR_NEXT);

			_uc.WriteU32(block + HDR_USER_SIZE, FREE_FLAG);

			if (next != 0 && IsBlockFree(next))
				MergeBlocks(block, next);
			if (prev != 0 && IsBlockFree(prev))
				MergeBlocks(prev, block);
		}

		public uint GetPtrSize(uint ptr) {
			var block = ptr - SIZE_OF_HEADER;
			return _uc.ReadU32(block + HDR_USER_SIZE);
		}

		public bool SetPtrSize(uint ptr, uint newSize) {
			var block = ptr - SIZE_OF_HEADER;
			var currentSize = _uc.ReadU32(block + HDR_USER_SIZE);

			if (newSize == currentSize)
				return true;

			// Occupy all room up to the next used block
			var next = _uc.ReadU32(block + HDR_NEXT);
			if (next != 0 && IsBlockFree(next)) {
				MergeBlocks(block, next);
			}

			// Can we fit the desired size in?
			var maxSize = _uc.ReadU32(block + HDR_BLOCK_SIZE);
			var success = false;
			if (newSize < (maxSize - SIZE_OF_HEADER)) {
				_uc.WriteU32(block + HDR_USER_SIZE, newSize);
				if (newSize > currentSize) {
					for (var i = currentSize; i < newSize; i++) {
						_uc.WriteU8(ptr + i, 0);
					}
				}
				success = true;
			}

			ShrinkUsedBlockBySplitting(block);
			return success;
		}

		bool IsBlockFree(uint block) {
			var userSize = _uc.ReadU32(block + HDR_USER_SIZE);
			return (userSize & FREE_FLAG) == FREE_FLAG;
		}

		uint? FindFreeBlock(uint minSize) {
			var block = _lastBlock;

			while (block != 0) {
				var blockSize = _uc.ReadU32(block + HDR_BLOCK_SIZE);
				if (IsBlockFree(block) && blockSize >= (SIZE_OF_HEADER + minSize)) {
					return block;
				}
				block = _uc.ReadU32(block + HDR_PREV);
			}

			return null;
		}

		void ShrinkUsedBlockBySplitting(uint block) {
			var userSize = _uc.ReadU32(block + HDR_USER_SIZE);
			var blockSize = _uc.ReadU32(block + HDR_BLOCK_SIZE);

			var minBlockSize = SIZE_OF_HEADER + ((userSize + 0xFu) & ~0xFu);
			var freeSpace = blockSize - minBlockSize;
			if (freeSpace < (SIZE_OF_HEADER + 0x10)) {
				// too small to bother splitting!
				return;
			}

			var next = _uc.ReadU32(block + HDR_NEXT);

			// Update used block
			_uc.WriteU32(block + HDR_BLOCK_SIZE, minBlockSize);

			// Mark new block as free
			var secondBlock = block + minBlockSize;
			_uc.WriteU32(secondBlock + HDR_USER_SIZE, FREE_FLAG);
			_uc.WriteU32(secondBlock + HDR_BLOCK_SIZE, freeSpace);

			// Set up linkages
			_uc.WriteU32(block + HDR_NEXT, secondBlock);
			_uc.WriteU32(secondBlock + HDR_PREV, block);
			_uc.WriteU32(secondBlock + HDR_NEXT, next);

			if (next == 0) {
				_lastBlock = secondBlock;
			} else {
				_uc.WriteU32(next + HDR_PREV, secondBlock);
			}
		}

		void MergeBlocks(uint a, uint b) {
			var blockSizeA = _uc.ReadU32(a + HDR_BLOCK_SIZE);
			var blockSizeB = _uc.ReadU32(b + HDR_BLOCK_SIZE);
			if ((a + blockSizeA) != b)
				throw new InvalidOperationException($"bad block merge: a={a:X8} (size={blockSizeA:X8}) + b={b:X8} (size={blockSizeB:X8})");

			var combinedBlockSize = blockSizeA + blockSizeB;
			_uc.WriteU32(a + HDR_BLOCK_SIZE, combinedBlockSize);

			var next = _uc.ReadU32(b + HDR_NEXT);
			if (next == 0) {
				_lastBlock = a;
			} else {
				_uc.WriteU32(next + HDR_PREV, a);
			}
			_uc.WriteU32(a + HDR_NEXT, next);
		}
	}
}
