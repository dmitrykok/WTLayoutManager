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
#include <sstream>
#include <iomanip>
#include <msclr/marshal_cppstd.h>

using namespace msclr::interop;
using namespace WTLayoutManager::Services;

// Wide → UTF-8  (or CP_ACP if you prefer)
std::string WideToUtf8(const std::wstring& ws)
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
 * Launches a process with a custom environment block.
 * Returns the exit code of the process.
 * Throws an exception if the process could not be started.
 * @param applicationPath Path to the application executable
 * @param commandLine Command line arguments
 * @param envBlock Encoded environment block (e.g. "VAR1=Value1;VAR2=Value2")
 */
int ProcessLauncher::LaunchProcess(System::String^ applicationPath, System::String^ commandLine, System::String^ envBlock, System::String^ hookPath)
{
    marshal_context^ ctx = gcnew marshal_context();

    const wchar_t* appPath = ctx->marshal_as<const wchar_t*>(applicationPath);
    const wchar_t* cmdRaw = ctx->marshal_as<const wchar_t*>(commandLine);
    const wchar_t* hookRaw = ctx->marshal_as<const wchar_t*>(hookPath);

    // 1. ----- DUPLICATE COMMAND LINE --------------------------------------
    size_t cmdLen = wcslen(cmdRaw) + 1;
    std::unique_ptr<wchar_t[]> cmdLine(new wchar_t[cmdLen]);
    if (FAILED(StringCchCopyW(cmdLine.get(), cmdLen, cmdRaw))) 
    {
        std::wstring err = WinApiHelpers::GetLastErrorMessage();
        delete ctx;
        throw gcnew System::Exception(gcnew System::String(err.c_str()));
    }

    // 2. ----- SPLIT envBlock → vector<wstring> ---------------------------
    std::vector<std::wstring> additional;

    if (!System::String::IsNullOrEmpty(envBlock))
    {
        array<System::String^>^ parts = envBlock->Split(L';');

        for each (System::String^ part in parts)
        {
            if (System::String::IsNullOrWhiteSpace(part)) 
                continue;      // skip empty

            additional.emplace_back(ctx->marshal_as<const wchar_t*>(part));
        }
    }

    // 3. ----- MERGE with parent environment ------------------------------
    std::unique_ptr<wchar_t[]> merged;
    DWORD dwCreationFlags = 0;
    if (!additional.empty())
    {
        merged.reset(WinApiHelpers::CreateMergedEnvironmentBlock(additional));
        dwCreationFlags = CREATE_UNICODE_ENVIRONMENT;
    }

    STARTUPINFOEXW si{ sizeof(si) };
    PROCESS_INFORMATION pi = { 0 };

    std::wstring hookW(hookRaw);
    std::string hook = WideToUtf8(hookW);
    BOOL success = WinApiHelpers::DetourCreateProcessWithDllExWrap(
        appPath,            // or retrieved from argv
        cmdLine.get(),      // command line (inherit)
        nullptr, nullptr,   // security attrs
        FALSE,              // inherit handles
        dwCreationFlags | CREATE_SUSPENDED,
        merged.get(),       // Custom environment block
        nullptr,            // cwd
        &si.StartupInfo,
        &pi,
        hook.c_str(),       // *** injected DLL
        nullptr);           // default create-process routine

    std::wstring err = WinApiHelpers::GetLastErrorMessage();
    if (!success)
    {
        std::wstring err = WinApiHelpers::GetLastErrorMessage();
        delete ctx;
        throw gcnew System::Exception(gcnew System::String(err.c_str()));
    }

    //success = DebugActiveProcess(pi.dwProcessId);
    success = ResumeThread(pi.hThread);

    // Wait for the process to exit.
    WaitForSingleObject(pi.hProcess, INFINITE);

    merged.reset();
    cmdLine.reset();

    DWORD exitCode = 0;
    if (!GetExitCodeProcess(pi.hProcess, &exitCode))
    {
        std::wstring err = WinApiHelpers::GetLastErrorMessage();
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
        delete ctx;
        throw gcnew System::Exception(gcnew System::String(err.c_str()));
    }

	if (exitCode != 0)
	{
        std::wstringstream ss;
        ss << L"Process exited with code: 0x"
            << std::setw(8) << std::setfill(L'0')
            << std::uppercase << std::hex << exitCode;

        std::wstring err = ss.str();
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
        delete ctx;
		throw gcnew System::Exception(gcnew System::String(err.c_str()));
	}

    CloseHandle(pi.hProcess);
    CloseHandle(pi.hThread);
    delete ctx;
    return static_cast<int>(exitCode);
}

/**
 * Launches an elevated process via a launcher executable.
 * The launcher (with a UAC manifest) starts the target process using the provided encoded environment block.
 * Returns the exit code of the target process.
 * Throws an exception if the launcher could not be started.
 * @param launcherPath Path to the launcher executable
 * @param applicationPath Path to the target application executable
 * @param commandLine Command line arguments
 * @param envBlock Encoded environment block (e.g. "VAR1=Value1;VAR2=Value2")
 */
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
