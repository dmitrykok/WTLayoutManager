#include "pch.h"
#include <windows.h>
#include <tchar.h>
#include <strsafe.h>
#include <iostream>
#include <string>
#include <vector>


// Helper to retrieve the last error message.
std::wstring GetLastErrorMessage() {
    DWORD errorCode = GetLastError();
    LPWSTR messageBuffer = nullptr;
    size_t size = FormatMessageW(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
        NULL, errorCode, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
        (LPWSTR)&messageBuffer, 0, NULL);
    std::wstring message(messageBuffer, size);
    LocalFree(messageBuffer);
    return message;
}

// Helper: Merge parent's environment with additional variables.
LPWSTR CreateMergedEnvironmentBlock(const std::vector<std::wstring>& additionalVars)
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

int wmain(int argc, wchar_t* argv[])
{
    // We expect exactly three parameters:
    // argv[1] = target application path
    // argv[2] = target command line
    // argv[3] = encoded environment block (e.g. "VAR1=Value1;VAR2=Value2")
    if (argc < 4)
    {
        std::wcerr << L"Usage: LauncherExe.exe <applicationPath> <commandLine> <envBlock>" << std::endl;
        return -1;
    }

    LPCWSTR targetApp = argv[1];
    LPWSTR targetCmdLine = argv[2];
    LPCWSTR envParam = argv[3];

    // Decode the environment block.
    // Here we assume the environment variables are separated by semicolons.
    // For example: "MY_VAR1=Value1;MY_VAR2=Value2"
    std::wstring envStr(envParam);
    DWORD dwCreationFlags = 0;
    LPWSTR envCopy = NULL;
    if (envStr.size())
    {
        std::vector<std::wstring> additional = { envStr.c_str() };
        envCopy = CreateMergedEnvironmentBlock(additional);
        dwCreationFlags = CREATE_UNICODE_ENVIRONMENT;
    }

    STARTUPINFOW si = { 0 };
    si.cb = sizeof(si);
    PROCESS_INFORMATION pi = { 0 };

    BOOL success = CreateProcessW(
        targetApp,
        targetCmdLine, // Command line (can be modified as needed)
        NULL,
        NULL,
        FALSE,
        dwCreationFlags,
        envCopy,      // Custom environment block
        NULL,
        &si,
        &pi
    );

    if (envCopy)
    {
        delete[] envCopy;
    }

    if (!success)
    {
        std::wcerr << L"CreateProcess failed." << std::endl;
        return -1;
    }

    // Wait for the target process to exit.
    WaitForSingleObject(pi.hProcess, INFINITE);
    DWORD exitCode = 0;
    if (!GetExitCodeProcess(pi.hProcess, &exitCode))
    {
        std::wcerr << L"GetExitCodeProcess failed." << std::endl;
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
        return -1;
        //exitCode = GetLastError();
    }
    CloseHandle(pi.hProcess);
    CloseHandle(pi.hThread);
    return static_cast<int>(exitCode);
}
