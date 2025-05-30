// new.cpp
#include "pch.h"
#include "new.h"

/**
 * @brief Convert a _MemBlockHeader* to the associated block of memory.
 *
 * @param[in] header The _MemBlockHeader to convert.
 *
 * @returns A pointer to the first byte of the block of memory associated with the provided header.
 */
inline unsigned char* block_from_header(_MemBlockHeader* const header) noexcept {
	return reinterpret_cast<unsigned char*>(header + 1);
}

/**
 * @brief Convert a block of memory to the associated _MemBlockHeader*.
 *
 * @param[in] block The block of memory to convert.
 *
 * @returns A pointer to the _MemBlockHeader associated with the provided block of memory.
 */
inline _MemBlockHeader* header_from_block(void const* const block) noexcept {
	return static_cast<_MemBlockHeader*>(const_cast<void*>(block)) - 1;
}

/**
 * @brief Convert a _MemBlockHeader* to the associated footer.
 *
 * @param[in] header The _MemBlockHeader to convert.
 *
 * @returns A pointer to the footer associated with the provided header.
 */
inline _MemBlockFooter* footer_from_header(_MemBlockHeader* const header) noexcept {
	return reinterpret_cast<_MemBlockFooter*>(
		reinterpret_cast<unsigned char*>(
			const_cast<_MemBlockHeader*>(header)) + sizeof(_MemBlockHeader) + header->_data_size);
}

/**
 * @brief Checks whether the given pointer is a valid heap pointer or not.
 *
 * @param[in] block The pointer to check.
 *
 * @returns TRUE if the pointer is a valid heap pointer, FALSE otherwise.
 */
BOOL __CRTDECL NewValidHeapPointer(void const* const block) noexcept
{
	_MemBlockHeader* header = NULL;
	if (block)
		header = header_from_block(block);

	return HeapValidate(GetProcessHeap(), 0, header);
}


/**
 * @brief Allocates a block of memory of the specified size using the heap.
 *
 * This function allocates a block of memory of the specified size and initializes its header and footer.
 * It uses HeapAlloc to allocate the memory and throws an exception if the allocation fails.
 *
 * @param[in] size The size of the memory block to allocate.
 *
 * @returns A pointer to the allocated memory block.
 */
_NODISCARD _Ret_notnull_ _Post_writable_byte_size_(size) _VCRT_ALLOCATOR
void* __CRTDECL operator new(size_t const size) {
	void* block = HeapAlloc(GetProcessHeap(), HEAP_GENERATE_EXCEPTIONS | HEAP_ZERO_MEMORY, size + sizeof(_MemBlockHeader) + sizeof(_MemBlockFooter));
	if (!block) {
		throw bad_heap_alloc();
	}
	_MemBlockHeader* header = reinterpret_cast<_MemBlockHeader*>(block);
	header->_block_guard = 0xdeadbeef;
	header->_block_use = _NORMAL_BLOCK;  // Use appropriate constant if defined or your own value
	header->_data_size = size;
	_MemBlockFooter* footer = footer_from_header(header);
	footer->_block_guard = 0xdeadbeef;
	return block_from_header(header);
}

/**
 * @brief Frees a block of memory previously allocated by operator new.
 *
 * This function checks if the given pointer is non-null and validates it as a heap pointer.
 * It verifies the block's integrity using guard values and updates the block's header and footer
 * to indicate it's been freed. The function uses HeapFree to release the memory back to the process heap.
 * If any integrity checks fail or HeapFree fails, it will trigger a debug break.
 *
 * @param[in] ptr The pointer to the memory block to be freed.
 */
void __CRTDECL operator delete(void* const ptr) noexcept {
	if (!ptr) {
		return;
	}
	NewValidHeapPointer(ptr);
	_MemBlockHeader* header = header_from_block(ptr);
	if (header->_block_guard != 0xdeadbeef) {
		_CrtDbgBreak();
	}
	if (header->_block_use != _NORMAL_BLOCK) {
		_CrtDbgBreak();
	}
	_MemBlockFooter* footer = footer_from_header(header);
	if (footer->_block_guard != 0xdeadbeef) {
		_CrtDbgBreak();
	}
	footer->_block_guard = 0xdeadf00d;
	header->_block_guard = 0xdeadf00d;
	header->_block_use = _FREE_BLOCK;  // Use your defined constant for freed blocks
	header->_data_size = 0;
	__try
	{
		if (!HeapFree(GetProcessHeap(), 0, header))
		{
			throw bad_heap_free();
		}
	}
	__except (EXCEPTION_EXECUTE_HANDLER)
	{
		_CrtDbgBreak();
	}
}

/**
 * @brief Allocates an array of objects of the specified size using the heap.
 *
 * This function allocates an array of objects of the specified size and initializes its header and footer.
 * It uses HeapAlloc to allocate the memory and throws an exception if the allocation fails.
 *
 * @param[in] size The size of the memory block to allocate.
 *
 * @returns A pointer to the allocated memory block.
 */
_NODISCARD _Ret_notnull_ _Post_writable_byte_size_(size) _VCRT_ALLOCATOR
void* __CRTDECL operator new[](size_t const size) {
	void* block = HeapAlloc(GetProcessHeap(), HEAP_GENERATE_EXCEPTIONS | HEAP_ZERO_MEMORY, size + sizeof(_MemBlockHeader) + sizeof(_MemBlockFooter));
	if (!block) {
		throw bad_heap_alloc();
	}
	_MemBlockHeader* header = reinterpret_cast<_MemBlockHeader*>(block);
	header->_block_guard = 0xdeadbeef;
	header->_block_use = _NORMAL_BLOCK;
	header->_data_size = size;
	_MemBlockFooter* footer = footer_from_header(header);
	footer->_block_guard = 0xdeadbeef;
	return block_from_header(header);
}


/**
 * @brief Deallocates an array of objects previously allocated with operator new[].
 *
 * This function frees the memory block associated with the given pointer, performing
 * debug checks and heap management. It validates the memory block, marks it as freed,
 * and releases the memory using HeapFree.
 *
 * @param[in] ptr Pointer to the memory block to be deallocated. If nullptr, no action is taken.
 * @throws bad_heap_free If the heap deallocation fails.
 * @note This is a custom implementation with additional debug guards and validation.
 */
void __CRTDECL operator delete[](void* const ptr) noexcept {
	if (!ptr) {
		return;
	}
	NewValidHeapPointer(ptr);
	_MemBlockHeader* header = header_from_block(ptr);
	if (header->_block_guard != 0xdeadbeef) {
		_CrtDbgBreak();
	}
	if (header->_block_use != _NORMAL_BLOCK) {
		_CrtDbgBreak();
	}
	_MemBlockFooter* footer = footer_from_header(header);
	if (footer->_block_guard != 0xdeadbeef) {
		_CrtDbgBreak();
	}
	footer->_block_guard = 0xdeadf00d;
	header->_block_guard = 0xdeadf00d;
	header->_block_use = _FREE_BLOCK;
	header->_data_size = 0;
	__try
	{
		if (!HeapFree(GetProcessHeap(), 0, header))
		{
			throw bad_heap_free();
		}
	}
	__except (EXCEPTION_EXECUTE_HANDLER)
	{
		_CrtDbgBreak();
	}
}
