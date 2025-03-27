#include "pch.h"
#include <windows.h>
#include <tchar.h>
#include <strsafe.h>
#include <iostream>
#include <string>
#include <vector>
#include "WinApiHelpers.h"

using namespace WTLayoutManager::Services;

int wmain(int argc, wchar_t* argv[])
{
    // We expect exactly three parameters:
    // argv[1] = target application path
    // argv[2] = target command line
    // argv[3] = encoded environment block (e.g. "VAR1=Value1;VAR2=Value2")
    if (argc != 4)
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
        envCopy = WinApiHelpers::CreateMergedEnvironmentBlock(additional);
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
