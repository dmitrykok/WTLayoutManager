#include "pch.h"
#include "ProcessLauncherWrapper.h"
#include <windows.h>
#include <stdexcept>
#include <iostream>
#include <sstream>
#include <string>
#include <vector>
#include <memory>
#include <msclr/marshal_cppstd.h>
#include "WinApiHelpers.h"

using namespace msclr::interop;
using namespace WTLayoutManager::Services;

int ProcessLauncher::LaunchProcess(System::String^ applicationPath, System::String^ commandLine, System::String^ envBlock)
{
    std::wstring appPath = marshal_as<std::wstring>(applicationPath);
    std::wstring cmdLine = L"\"" + marshal_as<std::wstring>(commandLine) + L"\"";
    std::wstring env = marshal_as<std::wstring>(envBlock);

    // Duplicate the environment block into a modifiable buffer.
	DWORD dwCreationFlags = 0;
    std::unique_ptr<wchar_t[]> envCopy(nullptr);
    if (env.size())
    {
        std::vector<std::wstring> additional = { env.c_str() };
        envCopy.reset(WinApiHelpers::CreateMergedEnvironmentBlock(additional));
		dwCreationFlags = CREATE_UNICODE_ENVIRONMENT;
    }

    STARTUPINFOW si = { 0 };
    si.cb = sizeof(si);
    PROCESS_INFORMATION pi = { 0 };

    BOOL success = CreateProcessW(
        appPath.c_str(),                      // Application path
        const_cast<LPWSTR>(cmdLine.c_str()),  // Command line
        NULL,
        NULL,
        FALSE,
        dwCreationFlags,
        envCopy.get(),                              // Custom environment block
        NULL,
        &si,
        &pi
    );

    if (!success)
    {
        std::wstring err = WinApiHelpers::GetLastErrorMessage();
        throw gcnew System::Exception(gcnew System::String(err.c_str()));
    }

    // Wait for the process to exit.
    WaitForSingleObject(pi.hProcess, INFINITE);

    DWORD exitCode = 0;
    if (!GetExitCodeProcess(pi.hProcess, &exitCode))
    {
        std::wstring err = WinApiHelpers::GetLastErrorMessage();
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
        throw gcnew System::Exception(gcnew System::String(err.c_str()));
    }

	if (exitCode != 0)
	{
		std::wstring err = L"Process exited with code " + std::to_wstring(exitCode);
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
		throw gcnew System::Exception(gcnew System::String(err.c_str()));
	}

    CloseHandle(pi.hProcess);
    CloseHandle(pi.hThread);
    return static_cast<int>(exitCode);
}

int ProcessLauncher::LaunchProcessElevated(System::String^ launcherPath, System::String^ applicationPath, System::String^ commandLine, System::String^ envBlock)
{
    // Convert managed strings to std::wstring.
    std::wstring launcher = marshal_as<std::wstring>(launcherPath);
    std::wstring appPath = marshal_as<std::wstring>(applicationPath);
    std::wstring cmdLine = L"\"" + marshal_as<std::wstring>(commandLine) + L"\"";
    std::wstring env = marshal_as<std::wstring>(envBlock);

    // Helper lambda to properly quote an argument by escaping internal quotes.
    auto QuoteArgument = [](const std::wstring& arg) -> std::wstring {
        std::wstringstream result;
        result << L"\"";
        for (wchar_t ch : arg) {
            if (ch == L'\"') {
                result << L"\\\"";
            }
            else {
                result << ch;
            }
        }
        result << L"\"";
        return result.str();
    };

    std::wstring parameters = QuoteArgument(appPath) + L" " + QuoteArgument(cmdLine) + L" " + QuoteArgument(env);

    SHELLEXECUTEINFOW sei = { 0 };
    sei.cbSize = sizeof(sei);
    sei.fMask = SEE_MASK_NOCLOSEPROCESS;
    sei.lpVerb = L"runas"; // Request elevation (UAC prompt)
    sei.lpFile = launcher.c_str();
    sei.lpParameters = parameters.c_str();
    sei.nShow = SW_HIDE;

    if (!ShellExecuteEx(&sei))
    {
        std::wstring err = WinApiHelpers::GetLastErrorMessage();
        throw gcnew System::Exception(gcnew System::String(err.c_str()));
    }

    // Wait for the launcher executable to complete.
    WaitForSingleObject(sei.hProcess, INFINITE);
    DWORD exitCode = 0;
    if (!GetExitCodeProcess(sei.hProcess, &exitCode))
    {
        std::wstring err = WinApiHelpers::GetLastErrorMessage();
        CloseHandle(sei.hProcess);
        throw gcnew System::Exception(gcnew System::String(err.c_str()));
    }
        
    if (exitCode == -1)
	{
		// ShellExecuteEx failed.
		std::wstring err = WinApiHelpers::GetLastErrorMessage();
        CloseHandle(sei.hProcess);
		throw gcnew System::Exception(gcnew System::String(err.c_str()));
	}
    else if (exitCode != 0)
    {
        std::wstring err = L"Process exited with code " + std::to_wstring(exitCode);
        CloseHandle(sei.hProcess);
        throw gcnew System::Exception(gcnew System::String(err.c_str()));
    }

    CloseHandle(sei.hProcess);
    return static_cast<int>(exitCode);
}
