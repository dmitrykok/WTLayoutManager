#include "pch.h"
#include "WinApiHelpers.h"

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

    // Calculate the size of the parent's environment block.
    size_t parentSize = 0;
    for (LPWCH p = parentEnv; *p != L'\0'; p += wcslen(p) + 1) {
        parentSize += wcslen(p) + 1;
    }
    // parentSize now holds the total number of wchar_t's (excluding the final extra null)

    // Convert parent's environment block into a vector for easy manipulation.
    std::vector<std::wstring> envVars;
    for (LPWCH p = parentEnv; *p != L'\0'; p += wcslen(p) + 1) {
        envVars.push_back(p);
    }
    FreeEnvironmentStringsW(parentEnv);

    // Append or override with additional variables.
    // (For simplicity, we'll just append here.)
    for (const auto& var : additionalVars) {
        envVars.push_back(var);
    }

    // Calculate the total size needed for the merged environment block.
    size_t totalSize = 0;
    for (const auto& var : envVars) {
        totalSize += var.size() + 1; // plus null terminator for each
    }
    totalSize++; // final extra null terminator

    // Allocate the merged environment block.
    LPWSTR mergedEnv = new WCHAR[totalSize];
    WCHAR* cur = mergedEnv;
    for (const auto& var : envVars) {
        wcscpy_s(cur, var.size() + 1, var.c_str());
        cur += var.size() + 1;
    }
    *cur = L'\0'; // double null termination

    return mergedEnv;
}
