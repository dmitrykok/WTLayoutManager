#include "pch.h"
#include "WinApiHelpers.h"
#include <strsafe.h>

using namespace WTLayoutManager::Services;

// Helper to retrieve the last error message.
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

// Helper: Merge parent's environment with additional variables.
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
