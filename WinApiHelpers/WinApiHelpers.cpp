#include "pch.h"
#include "WinApiHelpers.h"
#include <strsafe.h>
#include <tlhelp32.h>
#include <detours.h>

using namespace WTLayoutManager::Services;

/**
 * \brief A RAII wrapper for SHELLEXECUTEINFOW.
 *
 * Automatically releases any resources allocated by ShellExecuteExW() when the
 * object is destroyed.
 */
shellexecuteinfow_raii::shellexecuteinfow_raii()
{
	ZeroMemory(&sei, sizeof(sei));
	sei.cbSize = sizeof(sei);
	sei.hProcess = INVALID_HANDLE_VALUE;
}
/**
 * \brief Destructor, releases any allocated resources.
 *
 * Automatically calls reset() to ensure any allocated resources are released.
 */
shellexecuteinfow_raii::~shellexecuteinfow_raii()
{
	reset();
}

/**
 * \brief Move constructor, takes ownership of the SHELLEXECUTEINFOW structure of \a other.
 *
 * After calling this constructor, the SHELLEXECUTEINFOW structure of \a other is left in an
 * "uninitialized" state, i.e. the \a hProcess member is set to \c INVALID_HANDLE_VALUE.
 */
shellexecuteinfow_raii::shellexecuteinfow_raii(shellexecuteinfow_raii&& other) noexcept
	: sei(other.sei)
{
	other.sei.hProcess = INVALID_HANDLE_VALUE;
}
shellexecuteinfow_raii& shellexecuteinfow_raii::operator=(shellexecuteinfow_raii&& other) noexcept
{
	if (this != &other)
	{
		reset();
		sei = other.sei;
		other.sei.hProcess = INVALID_HANDLE_VALUE;
	}
	return *this;
}

/**
 * \brief Resets the SHELLEXECUTEINFOW structure to an uninitialized state.
 *
 * Automatically calls CloseHandle() on the \a hProcess member if it is valid,
 * and sets it to \c INVALID_HANDLE_VALUE afterwards. This ensures that the
 * SHELLEXECUTEINFOW structure can be reused or safely destroyed.
 */
void shellexecuteinfow_raii::reset() noexcept
{
	if (sei.hProcess && sei.hProcess != INVALID_HANDLE_VALUE)
	{
		__try
		{
			::CloseHandle(sei.hProcess);
		}
		__except (EXCEPTION_EXECUTE_HANDLER) {} // ignore errors
		sei.hProcess = INVALID_HANDLE_VALUE;
	}
}

// implicit conversion when a SHELLEXECUTEINFOW* is required
shellexecuteinfow_raii::operator SHELLEXECUTEINFOW* () noexcept
{
	return &sei;
}

// --------------------------------------------------------------------------

/**
 * \brief Constructor, initializes the PROCESS_INFORMATION structure to an uninitialized state.
 *
 * Automatically zeros out the entire structure and sets the \a hProcess and \a hThread members
 * to \c INVALID_HANDLE_VALUE. This ensures that the PROCESS_INFORMATION structure can be safely
 * reused or destroyed.
 */
process_info_raii::process_info_raii()
{
	ZeroMemory(&pi, sizeof(pi));
	pi.hProcess = pi.hThread = INVALID_HANDLE_VALUE;
}
/**
 * \brief Destructor, releases any allocated resources.
 *
 * Automatically calls reset() to ensure any allocated resources are released.
 */
process_info_raii::~process_info_raii()
{
	reset();
}

/**
 * \brief Move constructor, takes ownership of the PROCESS_INFORMATION structure.
 *
 * Releases any allocated resources of the source object and takes ownership of the PROCESS_INFORMATION structure.
 * The source object is left in a reset state.
 */
process_info_raii::process_info_raii(process_info_raii&& other) noexcept
	: pi(other.pi)
{
	other.pi.hProcess = other.pi.hThread = INVALID_HANDLE_VALUE;
}
process_info_raii& process_info_raii::operator=(process_info_raii&& other) noexcept
{
	if (this != &other)
	{
		reset();
		pi = other.pi;
		other.pi.hProcess = other.pi.hThread = INVALID_HANDLE_VALUE;
	}
	return *this;
}

/**
 * \brief Resets the PROCESS_INFORMATION structure to an uninitialized state.
 *
 * Automatically calls CloseHandle() on the \a hProcess and \a hThread members if they are valid,
 * and sets them to \c INVALID_HANDLE_VALUE afterwards. This ensures that the PROCESS_INFORMATION
 * structure can be reused or safely destroyed.
 */
void process_info_raii::reset() noexcept
{
	if (pi.hThread && pi.hThread != INVALID_HANDLE_VALUE)
	{
		__try
		{
			::CloseHandle(pi.hThread);
		}
		__except (EXCEPTION_EXECUTE_HANDLER) {} // ignore errors
		pi.hThread = INVALID_HANDLE_VALUE;
	}
	if (pi.hProcess && pi.hProcess != INVALID_HANDLE_VALUE)
	{
		__try
		{
			::CloseHandle(pi.hProcess);
		}
		__except (EXCEPTION_EXECUTE_HANDLER) {} // ignore errors
		pi.hProcess = INVALID_HANDLE_VALUE;
	}
}

// implicit conversion when a PROCESS_INFORMATION* is required
process_info_raii::operator PROCESS_INFORMATION* () noexcept
{
	return &pi;
}

// --------------------------------------------------------------------------

/**
 * Converts a wide string (std::wstring) to a UTF-8 encoded string (std::string).
 *
 * @param ws The wide string to be converted to UTF-8.
 * @return A std::string containing the UTF-8 encoded representation of the input wide string.
 */

std::string WinApiHelpers::WideToUtf8(const std::wstring& ws)
{
	int len = WideCharToMultiByte(CP_UTF8, 0,
		ws.data(), (int)ws.size(),
		nullptr, 0, nullptr, nullptr);
	std::string s(len, 0);
	WideCharToMultiByte(CP_UTF8, 0,
		ws.data(), (int)ws.size(),
		s.data(), len, nullptr, nullptr);
	return s;
}

/**
 * Retrieves the last error message from the system as a std::wstring.
 *
 * Uses the current thread's last error code and formats it into a human-readable string.
 *
 * @return The formatted error message corresponding to the last error code.
 */
std::wstring WinApiHelpers::GetLastErrorMessage() {
	DWORD errorCode = GetLastError();
	LPWSTR messageBuffer = nullptr;
	size_t size = FormatMessage(
		FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
		NULL,
		errorCode,
		MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
		(LPWSTR)&messageBuffer,
		0, NULL);
	std::wstring message(messageBuffer, size);
	LocalFree(messageBuffer);
	return message;
}

/**
 * Creates a merged environment block by combining the current process's environment variables
 * with additional variables provided in the input vector.
 *
 * Each variable is represented as a null-terminated string, and the resulting block is double-null terminated.
 * Returns a pointer to the newly allocated environment block, or nullptr on failure.
 *
 * @param additionalVars Vector of additional environment variables to append.
 * @return Pointer to the merged environment block, or nullptr if allocation fails.
 */
LPWSTR WinApiHelpers::CreateMergedEnvironmentBlock(const std::vector<std::wstring>& additionalVars)
{
	// Get the parent's environment block.
	LPWCH parentEnv = GetEnvironmentStringsW();
	if (!parentEnv) {
		return nullptr;
	}

	// Convert parent's environment block into a vector for easy manipulation.
	std::vector<std::wstring> envVars;
	LPTSTR lpszVariable = (LPTSTR)parentEnv;
	while (*lpszVariable)
	{
		envVars.push_back(std::wstring(lpszVariable));
		int len = lstrlen(lpszVariable) + 1;
		lpszVariable += len;
	}
	// Calculate the size of the parent's environment block.
	// parentSize now holds the total number of wchar_t's (excluding the final extra null)
	size_t parentSize = lpszVariable - parentEnv;

	FreeEnvironmentStringsW(parentEnv);

	// Append or override with additional variables.
	// (For simplicity, we'll just append here.)
	// Calculate the total size needed for the merged environment block.
	size_t totalSize = parentSize;
	for (const auto& var : additionalVars) {
		size_t len = var.length() + 1;
		envVars.push_back(var);
		totalSize += len;
	}
	totalSize++; // final extra null terminator

	// Allocate the merged environment block.
	LPWSTR mergedEnv = new WCHAR[totalSize];
	WCHAR* cur = mergedEnv;
	for (const auto& var : envVars) {
		size_t len = var.length() + 1;
		if (FAILED(StringCchCopy(cur, len, var.c_str()))) {
			delete[] mergedEnv;
			return nullptr;
		}
		cur += len;
	}
	*cur = (TCHAR)0; // double null termination

	return mergedEnv;
}

/**
 * A wrapper around DetourCreateProcessWithDllExW that takes a void* instead of a PDETOUR_CREATE_PROCESS_ROUTINEW.
 *
 * @param[in] lpApplicationName The name of the module to be executed.
 * @param[inout] lpCommandLine The command line to be executed.
 * @param[in] lpProcessAttributes The process security attributes.
 * @param[in] lpThreadAttributes The thread security attributes.
 * @param[in] bInheritHandles Whether the new process inherits the handles of the current process.
 * @param[in] dwCreationFlags The creation flags.
 * @param[in] lpEnvironment The environment block.
 * @param[in] lpCurrentDirectory The current directory.
 * @param[in] lpStartupInfo The STARTUPINFO structure.
 * @param[out] lpProcessInformation The PROCESS_INFORMATION structure.
 * @param[in] lpDllName The name of the DLL to load.
 * @param[in] pfCreateProcessW The function to call to create the new process.
 * @return Whether the call was successful.
 */
BOOL WinApiHelpers::DetourCreateProcessWithDllExWrap(
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
	_In_opt_ void* pfCreateProcessW)
{
	return DetourCreateProcessWithDllExW(
		lpApplicationName,
		lpCommandLine,
		lpProcessAttributes,
		lpThreadAttributes,
		bInheritHandles,
		dwCreationFlags,
		lpEnvironment,
		lpCurrentDirectory,
		lpStartupInfo,
		lpProcessInformation,
		lpDllName,
		reinterpret_cast<PDETOUR_CREATE_PROCESS_ROUTINEW>(pfCreateProcessW)
	);
}

/**
 * A wrapper around the Win32 Sleep function.
 *
 * @param[in] dwMilliseconds The time to sleep in milliseconds.
 */
void WinApiHelpers::Sleep(_In_ DWORD dwMilliseconds)
{
	::Sleep(dwMilliseconds);
}

/**
 * Finds the Windows Terminal process launched by the given process ID.
 *
 * @param[in] wtPid The process ID of the process that launched Windows Terminal.
 * @return A handle to the Windows Terminal process, or an empty HandlePtr if it's not found.
 */
HandlePtr WinApiHelpers::GetWindowsTerminalHandle(DWORD wtPid)
{
	DWORD parentPid = wtPid;
	HANDLE hReal = nullptr;
	for (int i = 0; i < 60 && !hReal; ++i) {
		HANDLE hSnap = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
		if (hSnap == INVALID_HANDLE_VALUE)
		{
			WinApiHelpers::Sleep(50);
			continue;
		}
		PROCESSENTRY32 pe{ sizeof(pe) };
		for (BOOL ok = Process32First(hSnap, &pe); ok; ok = Process32Next(hSnap, &pe)) {
			if (pe.th32ParentProcessID == parentPid
				&& _wcsicmp(pe.szExeFile, L"WindowsTerminal.exe") == 0)
			{
				// open with SYNCHRONIZE so we can wait on it:
				DWORD rights = SYNCHRONIZE | PROCESS_QUERY_LIMITED_INFORMATION;
				hReal = OpenProcess(rights, FALSE, pe.th32ProcessID);
				break;
			}
		}
		CloseHandle(hSnap);
		if (!hReal)
		{
			WinApiHelpers::Sleep(50);
		}
	}

	return HandlePtr(hReal);
}
