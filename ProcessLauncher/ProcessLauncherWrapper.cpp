#include "pch.h"
#include "new.h"
#include "WinApiHelpers.h"
#include "ProcessLauncherWrapper.h"
#include <windows.h>
#include <strsafe.h>
#include <stdexcept>
#include <iostream>
#include <sstream>
#include <string>
#include <vector>
#include <memory>
#include <crtdbg.h>
#include <msclr/marshal_cppstd.h>

using namespace msclr::interop;
using namespace WTLayoutManager::Services;

int ProcessLauncher::LaunchProcess(System::String^ applicationPath, System::String^ commandLine, System::String^ envBlock)
{
    marshal_context^ context = gcnew marshal_context();
    const wchar_t* appPath = context->marshal_as<const wchar_t*>(applicationPath);
    const wchar_t* cmdLine = context->marshal_as<const wchar_t*>(commandLine);
    const wchar_t* env = context->marshal_as<const wchar_t*>(envBlock);

    int len = lstrlen(cmdLine) + 1;
	std::unique_ptr<wchar_t[]> cmdLineCopy(new wchar_t[len]);
    if (FAILED(StringCchCopy(cmdLineCopy.get(), len, cmdLine))) {
        std::wstring err = WinApiHelpers::GetLastErrorMessage();
        delete context;
        throw gcnew System::Exception(gcnew System::String(err.c_str()));
    }

    // Duplicate the environment block into a modifiable buffer.
	DWORD dwCreationFlags = 0;
    std::unique_ptr<wchar_t[]> envCopy(nullptr);
    if (lstrlen(env))
    {
        std::vector<std::wstring> additional = { env };
        envCopy.reset(WinApiHelpers::CreateMergedEnvironmentBlock(additional));
		dwCreationFlags = CREATE_UNICODE_ENVIRONMENT;
    }

    STARTUPINFOW si = { 0 };
    si.cb = sizeof(si);
    PROCESS_INFORMATION pi = { 0 };

    BOOL success = CreateProcessW(
        appPath,                      // Application path
        cmdLineCopy.get(),  // Command line
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
        delete context;
        throw gcnew System::Exception(gcnew System::String(err.c_str()));
    }

    // Wait for the process to exit.
    WaitForSingleObject(pi.hProcess, INFINITE);
    SleepEx(3000, FALSE); // Wait a bit more to ensure the target process has started.

	envCopy.reset();
	cmdLineCopy.reset();

    DWORD exitCode = 0;
    if (!GetExitCodeProcess(pi.hProcess, &exitCode))
    {
        std::wstring err = WinApiHelpers::GetLastErrorMessage();
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
        delete context;
        throw gcnew System::Exception(gcnew System::String(err.c_str()));
    }

	if (exitCode != 0)
	{
		std::wstring err = L"Process exited with code " + std::to_wstring(exitCode);
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
        delete context;
		throw gcnew System::Exception(gcnew System::String(err.c_str()));
	}

    CloseHandle(pi.hProcess);
    CloseHandle(pi.hThread);
    delete context;
    return static_cast<int>(exitCode);
}

int ProcessLauncher::LaunchProcessElevated(System::String^ launcherPath, System::String^ applicationPath, System::String^ commandLine, System::String^ envBlock)
{
    marshal_context^ context = gcnew marshal_context();
    const wchar_t* _launcher = context->marshal_as<const wchar_t*>(launcherPath);
    const wchar_t* _appPath = context->marshal_as<const wchar_t*>(applicationPath);
    const wchar_t* _cmdLine = context->marshal_as<const wchar_t*>(commandLine);
    const wchar_t* _env = context->marshal_as<const wchar_t*>(envBlock);

	std::wstring launcher(_launcher);
	std::wstring appPath(_appPath);
	std::wstring cmdLine(_cmdLine);
	std::wstring env(_env);

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
        delete context;
        throw gcnew System::Exception(gcnew System::String(err.c_str()));
    }

    // Wait for the launcher executable to complete.
    WaitForSingleObject(sei.hProcess, INFINITE);
    SleepEx(3000, FALSE); // Wait a bit more to ensure the target process has started.

    DWORD exitCode = 0;
    if (!GetExitCodeProcess(sei.hProcess, &exitCode))
    {
        std::wstring err = WinApiHelpers::GetLastErrorMessage();
        CloseHandle(sei.hProcess);
        delete context;
        throw gcnew System::Exception(gcnew System::String(err.c_str()));
    }
        
    if (exitCode == -1)
	{
		// ShellExecuteEx failed.
		std::wstring err = WinApiHelpers::GetLastErrorMessage();
        CloseHandle(sei.hProcess);
        delete context;
		throw gcnew System::Exception(gcnew System::String(err.c_str()));
	}
    else if (exitCode != 0)
    {
        std::wstring err = L"Process exited with code " + std::to_wstring(exitCode);
        CloseHandle(sei.hProcess);
        delete context;
        throw gcnew System::Exception(gcnew System::String(err.c_str()));
    }

    CloseHandle(sei.hProcess);
    delete context;
    return static_cast<int>(exitCode);
}
