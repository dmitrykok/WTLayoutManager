#include "pch.h"
#include "WinApiHelpers.h"
#include <strsafe.h>
#include <tlhelp32.h>
#include <detours.h>

using namespace WTLayoutManager::Services;

shellexecuteinfow_raii::shellexecuteinfow_raii()
{
    ZeroMemory(&sei, sizeof(sei));
    sei.cbSize = sizeof(sei);
}
shellexecuteinfow_raii::~shellexecuteinfow_raii()
{
    reset();
}

shellexecuteinfow_raii::shellexecuteinfow_raii(shellexecuteinfow_raii&& other) noexcept
    : sei(other.sei)
{
    other.sei.hProcess = nullptr;
}
shellexecuteinfow_raii& shellexecuteinfow_raii::operator=(shellexecuteinfow_raii&& other) noexcept
{
    if (this != &other)
    {
        reset();
        sei = other.sei;
        other.sei.hProcess = nullptr;
    }
    return *this;
}

void shellexecuteinfow_raii::reset() noexcept
{
    if (sei.hProcess) { CloseHandle(sei.hProcess);  sei.hProcess = nullptr; }
}

// implicit conversion when a SHELLEXECUTEINFOW* is required
shellexecuteinfow_raii::operator SHELLEXECUTEINFOW* () noexcept
{
    return &sei;
}

// --------------------------------------------------------------------------

process_info_raii::process_info_raii()
{
    ZeroMemory(&pi, sizeof(pi));
}
process_info_raii::~process_info_raii()
{
    reset();
}

process_info_raii::process_info_raii(process_info_raii&& other) noexcept
    : pi(other.pi)
{
    other.pi.hProcess = other.pi.hThread = nullptr;
}
process_info_raii& process_info_raii::operator=(process_info_raii&& other) noexcept
{
    if (this != &other)
    {
        reset();
        pi = other.pi;
        other.pi.hProcess = other.pi.hThread = nullptr;
    }
    return *this;
}

void process_info_raii::reset() noexcept
{
    if (pi.hThread) { CloseHandle(pi.hThread);   pi.hThread = nullptr; }
    if (pi.hProcess) { CloseHandle(pi.hProcess);  pi.hProcess = nullptr; }
}

// implicit conversion when a PROCESS_INFORMATION* is required
process_info_raii::operator PROCESS_INFORMATION* () noexcept
{
    return &pi;
}

// --------------------------------------------------------------------------

// Wide → UTF-8  (or CP_ACP if you prefer)
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

void WinApiHelpers::Sleep(_In_ DWORD dwMilliseconds)
{
	::Sleep(dwMilliseconds);
}

HANDLE WinApiHelpers::GetWindowsTerminalHandle(DWORD wtPid)
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
    return hReal;
}
