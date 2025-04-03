// new.cpp
#include "pch.h"
#include "new.h"

inline unsigned char* block_from_header(_MemBlockHeader* const header) noexcept {
    return reinterpret_cast<unsigned char*>(header + 1);
}

inline _MemBlockHeader* header_from_block(void const* const block) noexcept {
    return static_cast<_MemBlockHeader*>(const_cast<void*>(block)) - 1;
}

inline _MemBlockFooter* footer_from_header(_MemBlockHeader* const header) noexcept {
    return reinterpret_cast<_MemBlockFooter*>(
        reinterpret_cast<unsigned char*>(
            const_cast<_MemBlockHeader*>(header)) + sizeof(_MemBlockHeader) + header->_data_size);
}

BOOL __CRTDECL NewValidHeapPointer(void const* const block) noexcept
{
    _MemBlockHeader* header = NULL;
    if (block)
        header = header_from_block(block);

    return HeapValidate(GetProcessHeap(), 0, header);
}

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
