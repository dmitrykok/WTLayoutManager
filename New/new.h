// GlobalNewDelete.h
#pragma once

#include <cstdlib>
#include <new>
#include <windows.h>
#include <crtdbg.h>

extern "C++" {

    _VCRT_EXPORT_STD class _NODISCARD bad_heap_alloc
        : public std::exception
    {
    public:

        bad_heap_alloc() noexcept
            : exception("bad heap allocation", 1)
        {
        }

    private:

        bad_heap_alloc(char const* const _Message) noexcept
            : exception(_Message, 1)
        {
        }
    };

    _VCRT_EXPORT_STD class _NODISCARD bad_heap_free
        : public std::exception
    {
    public:

        bad_heap_free() noexcept
            : exception("bad heap free", 1)
        {
        }

    private:

        bad_heap_free(char const* const _Message) noexcept
            : exception(_Message, 1)
        {
        }
    };

#pragma pack(push, _CRT_PACKING)

#pragma push_macro("new")
#undef new

    struct _MemBlockHeader {
        DWORD   _block_guard;
        int     _block_use;
        size_t  _data_size;
    };

    struct _MemBlockFooter {
        DWORD   _block_guard;
    };

    _VCRT_EXPORT_STD BOOL __CRTDECL NewValidHeapPointer(void const* const block = NULL) noexcept;

    // Export the global operators
    _VCRT_EXPORT_STD _NODISCARD _Ret_notnull_ _Post_writable_byte_size_(size) _VCRT_ALLOCATOR void* __CRTDECL operator new(size_t const size);
    _VCRT_EXPORT_STD void __CRTDECL operator delete(void* const ptr) noexcept;
    _VCRT_EXPORT_STD _NODISCARD _Ret_notnull_ _Post_writable_byte_size_(size) _VCRT_ALLOCATOR void* __CRTDECL operator new[](size_t const size);
    _VCRT_EXPORT_STD void __CRTDECL operator delete[](void* const ptr) noexcept;


#pragma pop_macro("new")

#pragma pack(pop)
}