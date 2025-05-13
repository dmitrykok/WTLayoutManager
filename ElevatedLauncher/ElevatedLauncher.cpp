#include "pch.h"
#include <windows.h>
#include <tchar.h>
#include <strsafe.h>
#include <iostream>
#include <string>
#include <vector>
#include <algorithm>   // std::find_if, std::remove_if
#include "WinApiHelpers.h"

using namespace WTLayoutManager::Services;

// Trim leading / trailing spaces or tabs – optional, but handy.
static inline void trim(std::wstring& s)
{
    auto notspace = [](wchar_t ch) { return ch != L' ' && ch != L'\t'; };

    s.erase(s.begin(), std::find_if(s.begin(), s.end(), notspace));         // left trim
    s.erase(std::find_if(s.rbegin(), s.rend(), notspace).base(), s.end());  // right trim
}

// Decode "Name=Value;Name2=Value2" → vector<wstring>
static std::vector<std::wstring> splitEnvBlock(const std::wstring& envStr)
{
    std::vector<std::wstring> result;

    std::wstring::size_type start = 0;
    while (start < envStr.length())
    {
        auto next = envStr.find(L';', start);
        if (next == std::wstring::npos)
        {
            next = envStr.length();          // last segment
        }

        std::wstring token = envStr.substr(start, next - start);
        trim(token);                         // optional

        if (!token.empty())
        {
            result.push_back(std::move(token));
        }

        start = next + 1;                    // skip the semicolon
    }
    return result;
}


/**
 * Entry point for the elevated launcher application.
 *
 * Launches a target process with optional custom environment variables.
 * Expects exactly 4 command-line arguments:
 * - Target application path
 * - Target command line
 * - Encoded environment block
 *
 * @param argc Number of command-line arguments
 * @param argv Array of command-line argument strings
 * @return Process exit code of the launched application, or -1 if an error occurs
 */
int wmain(int argc, wchar_t *argv[])
{
    // We expect exactly three parameters:
    // argv[1] = target application path
    // argv[2] = target command line
    // argv[3] = encoded environment block (e.g. "VAR1=Value1;VAR2=Value2")
    if (argc != 5)
    {
        std::wcerr << L"Usage: LauncherExe.exe <applicationPath> <commandLine> <envBlock> <hookLib>" << std::endl;
        return -1;
    }

    LPCWSTR targetApp = argv[1];
    LPWSTR targetCmdLine = argv[2];
    LPCWSTR envParam = argv[3];
    LPCWSTR hookParam = argv[4];

    // Decode the environment block.
    // Here we assume the environment variables are separated by semicolons.
    // For example: "MY_VAR1=Value1;MY_VAR2=Value2"
    std::wstring envStr(envParam);
    DWORD dwCreationFlags = NORMAL_PRIORITY_CLASS | CREATE_NEW_CONSOLE | CREATE_NEW_PROCESS_GROUP | CREATE_SUSPENDED;
    std::unique_ptr<wchar_t[]> envCopy(nullptr);
    if (envStr.size())
    {
        std::vector<std::wstring> additional = splitEnvBlock(envStr);
        envCopy.reset(WinApiHelpers::CreateMergedEnvironmentBlock(additional));
        dwCreationFlags |= CREATE_UNICODE_ENVIRONMENT;
    }

    STARTUPINFOEXW si{ sizeof(si) };
    si.StartupInfo.wShowWindow = SW_SHOWDEFAULT;
    process_info_raii pi;

    std::wstring hookW(hookParam);
    std::string hook = WinApiHelpers::WideToUtf8(hookW);
    BOOL success = WinApiHelpers::DetourCreateProcessWithDllExWrap(
        targetApp,          // or retrieved from argv
        targetCmdLine,      // command line (inherit)
        nullptr, nullptr,   // security attrs
        FALSE,              // inherit handles
        dwCreationFlags,
        envCopy.get(),      // Custom environment block
        nullptr,            // cwd
        &si.StartupInfo,
        (PROCESS_INFORMATION*)pi,
        hook.c_str(),       // *** injected DLL
        nullptr);           // default create-process routine

    if (!success)
    {
        std::wcerr << L"CreateProcess failed." << std::endl;
        return -1;
    }

    success = ResumeThread(pi.pi.hThread);

    HandlePtr piHandle = WinApiHelpers::GetWindowsTerminalHandle(pi.pi.dwProcessId);
    if (piHandle.get() == nullptr)
    {
        std::wcerr << L"GetWindowsTerminalHandle failed." << std::endl;
        return -1;
    }

    // Wait for the target process to exit.
    WaitForSingleObject(piHandle.get(), INFINITE);
    DWORD exitCode = 0;
    if (!GetExitCodeProcess(piHandle.get(), &exitCode))
    {
        std::wcerr << L"GetExitCodeProcess failed." << std::endl;
        return -1;
    }

    return static_cast<int>(exitCode);
}
