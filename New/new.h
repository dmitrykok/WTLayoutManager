// GlobalNewDelete.h
#pragma once

#include <cstdlib>
#include <new>
#include <windows.h>
#include <crtdbg.h>

extern "C++" {

    /// <summary>
    /// Custom exception class representing a failure in heap memory allocation.
    /// </summary>
    /// <remarks>
    /// This exception is thrown when a heap memory allocation operation fails.
    /// It is exported as a standard exception and marked with _NODISCARD to encourage error checking.
    /// </remarks>
    _VCRT_EXPORT_STD class _NODISCARD bad_heap_alloc
        : public std::exception
    {
    public:

        /// <summary>
        /// Default constructor for bad_heap_alloc exception.
        /// Initializes the exception with a default message indicating a heap allocation failure.
        /// </summary>
        /// <remarks>
        /// This constructor is marked noexcept to indicate it will not throw exceptions during construction.
        /// </remarks>
        bad_heap_alloc() noexcept
            : exception("bad heap allocation", 1)
        {
        }

    private:

        /// <summary>
        /// Constructor for bad_heap_alloc exception with a custom error message.
        /// </summary>
        /// <param name="_Message">A custom error message describing the heap allocation failure.</param>
        /// <remarks>
        /// This constructor is marked noexcept to indicate it will not throw exceptions during construction.
        /// </remarks>
        bad_heap_alloc(char const* const _Message) noexcept
            : exception(_Message, 1)
        {
        }
    };

    /// <summary>
    /// Custom exception class representing a failure in heap memory deallocation.
    /// </summary>
    /// <remarks>
    /// This exception is thrown when a heap memory free operation fails.
    /// It is exported as a standard exception and marked with _NODISCARD to encourage error checking.
    /// </remarks>
    _VCRT_EXPORT_STD class _NODISCARD bad_heap_free
        : public std::exception
    {
    public:

        /// <summary>
        /// Default constructor for bad_heap_free exception.
        /// Initializes the exception with a default message indicating a heap free operation failure.
        /// </summary>
        /// <remarks>
        /// This constructor is marked noexcept to indicate it will not throw exceptions during construction.
        /// </remarks>
        bad_heap_free() noexcept
            : exception("bad heap free", 1)
        {
        }

    private:

        /// <summary>
        /// Constructor for bad_heap_free exception with a custom error message.
        /// </summary>
        /// <param name="_Message">A custom error message describing the heap free operation failure.</param>
        /// <remarks>
        /// This constructor is marked noexcept to indicate it will not throw exceptions during construction.
        /// </remarks>
        bad_heap_free(char const* const _Message) noexcept
            : exception(_Message, 1)
        {
        }
    };

#pragma pack(push, _CRT_PACKING)

#pragma push_macro("new")
#undef new

    /// <summary>
    /// Internal memory block header structure used for tracking memory allocation details.
    /// </summary>
    /// <remarks>
    /// Contains metadata for a memory block, including a guard value, usage flag, and data size.
    /// This structure is typically used for debugging and memory management purposes.
    /// </remarks>
    /// <seealso cref="_MemBlockFooter"/>
    struct _MemBlockHeader {
        DWORD   _block_guard;
        int     _block_use;
        size_t  _data_size;
    };

    /// <summary>
    /// Internal memory block footer structure used for tracking memory allocation details.
    /// </summary>
    /// <remarks>
    /// Contains a guard value for memory block integrity verification.
    /// This structure is typically used for debugging and memory management purposes.
    /// </remarks>
    /// <seealso cref="_MemBlockHeader"/>
    struct _MemBlockFooter {
        DWORD   _block_guard;
    };

    /// <summary>
    /// Validates whether a given memory pointer is a valid heap pointer.
    /// </summary>
    /// <param name="block">A pointer to the memory block to validate. Can be NULL.</param>
    /// <returns>A boolean indicating whether the pointer is a valid heap pointer.</returns>
    /// <remarks>
    /// This function checks the validity of a memory pointer within the heap.
    /// When no block is provided (NULL), the behavior is implementation-defined.
    /// </remarks>
    _VCRT_EXPORT_STD BOOL __CRTDECL NewValidHeapPointer(void const* const block = NULL) noexcept;

    /// <summary>
    /// Allocates memory for a single object of specified size.
    /// </summary>
    /// <param name="size">The number of bytes to allocate.</param>
    /// <returns>A pointer to the allocated memory block.</returns>
    /// <exception cref="std::bad_alloc">Thrown if memory allocation fails.</exception>
    /// <remarks>
    /// This global operator is used by the new keyword for single object allocation.
    /// </remarks>
    _VCRT_EXPORT_STD _NODISCARD _Ret_notnull_ _Post_writable_byte_size_(size) _VCRT_ALLOCATOR void* __CRTDECL operator new(size_t const size);

    /// <summary>
    /// Deallocates memory previously allocated for a single object.
    /// </summary>
    /// <param name="ptr">Pointer to the memory block to be freed.</param>
    /// <remarks>
    /// This global operator is used by the delete keyword for single object deallocation.
    /// </remarks>
    _VCRT_EXPORT_STD void __CRTDECL operator delete(void* const ptr) noexcept;

    /// <summary>
    /// Allocates memory for an array of objects of specified size.
    /// </summary>
    /// <param name="size">The number of bytes to allocate for the array.</param>
    /// <returns>A pointer to the allocated memory block.</returns>
    /// <exception cref="std::bad_alloc">Thrown if memory allocation fails.</exception>
    /// <remarks>
    /// This global operator is used by the new[] keyword for array allocation.
    /// </remarks>
    _VCRT_EXPORT_STD _NODISCARD _Ret_notnull_ _Post_writable_byte_size_(size) _VCRT_ALLOCATOR void* __CRTDECL operator new[](size_t const size);

    /// <summary>
    /// Deallocates memory previously allocated for an array of objects.
    /// </summary>
    /// <param name="ptr">Pointer to the memory block to be freed.</param>
    /// <remarks>
    /// This global operator is used by the delete[] keyword for array deallocation.
    /// </remarks>
    _VCRT_EXPORT_STD void __CRTDECL operator delete[](void* const ptr) noexcept;


#pragma pop_macro("new")

#pragma pack(pop)
}