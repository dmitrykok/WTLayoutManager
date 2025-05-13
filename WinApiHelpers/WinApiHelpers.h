#pragma once

#include "new.h"
#include <windows.h>
#include <shellapi.h>
#include <string>
#include <vector>
#include <memory>

#ifdef WINAPIHELPERS_EXPORTS   // Define this in your pure C++ DLL project settings
#define WINAPIHELPERS_API __declspec(dllexport)
#else
#define WINAPIHELPERS_API __declspec(dllimport)
#endif

namespace WTLayoutManager {
	namespace Services {

        struct HandleCloser {
            void operator()(HANDLE h) const noexcept {
                if (h) ::CloseHandle(h);
            }
        };

        using HandlePtr = std::unique_ptr<
            std::remove_pointer_t<HANDLE>,   // = void
            HandleCloser        // function‐pointer deleter type
        >;

        struct shellexecuteinfow_raii
        {
            SHELLEXECUTEINFOW sei{ 0 };

            WINAPIHELPERS_API shellexecuteinfow_raii();
            WINAPIHELPERS_API ~shellexecuteinfow_raii();

            shellexecuteinfow_raii(const shellexecuteinfow_raii&) = delete;
            shellexecuteinfow_raii& operator=(const shellexecuteinfow_raii&) = delete;

            WINAPIHELPERS_API shellexecuteinfow_raii(shellexecuteinfow_raii&& other) noexcept;
            WINAPIHELPERS_API shellexecuteinfow_raii& operator=(shellexecuteinfow_raii&& other) noexcept;

            WINAPIHELPERS_API void reset() noexcept;

            // implicit conversion when a SHELLEXECUTEINFOW* is required
            WINAPIHELPERS_API operator SHELLEXECUTEINFOW* () noexcept;
        };

        struct process_info_raii
        {
            PROCESS_INFORMATION pi{ 0 };

            WINAPIHELPERS_API process_info_raii();
            WINAPIHELPERS_API ~process_info_raii();

            process_info_raii(const process_info_raii&) = delete;
            process_info_raii& operator=(const process_info_raii&) = delete;

            WINAPIHELPERS_API process_info_raii(process_info_raii&& other) noexcept;
            WINAPIHELPERS_API process_info_raii& operator=(process_info_raii&& other) noexcept;

            WINAPIHELPERS_API void reset() noexcept;

            // implicit conversion when a PROCESS_INFORMATION* is required
            WINAPIHELPERS_API operator PROCESS_INFORMATION* () noexcept;
        };

		/// <summary>
  		/// Provides utility methods for Windows API operations.
  		/// </summary>
  		/// <remarks>
  		/// Contains static methods for retrieving system error information and manipulating environment blocks.
  		/// </remarks>
		class WinApiHelpers
		{
		public:
			/// <summary>
			/// Retrieves the error message associated with the last Windows API error.
			/// </summary>
			/// <returns>A wide string containing the error message for the last system error.</returns>
			/// <remarks>
			/// This method uses the Windows API GetLastError() to obtain the current error code and FormatMessage() to convert it to a human-readable string.
			/// </remarks>
			WINAPIHELPERS_API static std::wstring GetLastErrorMessage();
			/// <summary>
   			/// Creates a merged environment block by combining existing environment variables with additional variables.
   			/// </summary>
   			/// <param name="additionalVars">A vector of wide strings containing additional environment variables to be added.</param>
   			/// <returns>A pointer to the newly created merged environment block (LPWSTR).</returns>
   			/// <remarks>
   			/// This method allows for dynamically creating an environment block with extra variables beyond the current process environment.
   			/// The caller is responsible for freeing the returned environment block using the appropriate Windows API function.
   			/// </remarks>
			WINAPIHELPERS_API static LPWSTR CreateMergedEnvironmentBlock(const std::vector<std::wstring>& additionalVars);

            WINAPIHELPERS_API static BOOL DetourCreateProcessWithDllExWrap(
                _In_opt_ LPCWSTR lpApplicationName,
                _Inout_opt_  LPWSTR lpCommandLine,
                _In_opt_ LPSECURITY_ATTRIBUTES lpProcessAttributes,
                _In_opt_ LPSECURITY_ATTRIBUTES lpThreadAttributes,
                _In_ BOOL bInheritHandles,
                _In_ DWORD dwCreationFlags,
                _In_opt_ LPVOID lpEnvironment,
                _In_opt_ LPCWSTR lpCurrentDirectory,
                _In_ LPSTARTUPINFOW lpStartupInfo,
                _Out_ LPPROCESS_INFORMATION lpProcessInformation,
                _In_ LPCSTR lpDllName,
                _In_opt_ void* pfCreateProcessW);

            WINAPIHELPERS_API static std::string WideToUtf8(const std::wstring& ws);

            WINAPIHELPERS_API static void Sleep(_In_ DWORD dwMilliseconds);

            WINAPIHELPERS_API static HandlePtr GetWindowsTerminalHandle(DWORD wtPid);
		};

	}
} // namespace WTLayoutManager::Services
