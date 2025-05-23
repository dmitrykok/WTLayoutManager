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
#include <tlhelp32.h>
#include <msclr/marshal_cppstd.h>
#include <msclr/auto_handle.h>

using namespace msclr::interop;
using namespace WTLayoutManager::Services;

// Helper function to format the process exit code.
static std::wstring FormatProcessExitCode(int exitCode)
{
	std::wstringstream ss;
	ss << L"Process exited with code: 0x"
		<< std::setw(8) << std::setfill(L'0')
		<< std::uppercase << std::hex << exitCode;
	return ss.str();
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
	msclr::auto_handle<msclr::interop::marshal_context> ctx(gcnew msclr::interop::marshal_context());
	const wchar_t* appPath = ctx->marshal_as<const wchar_t*>(applicationPath);
	const wchar_t* cmdRaw = ctx->marshal_as<const wchar_t*>(commandLine);
	const wchar_t* hookRaw = ctx->marshal_as<const wchar_t*>(hookPath);

	// 1. ----- DUPLICATE COMMAND LINE --------------------------------------
	size_t cmdLen = wcslen(cmdRaw) + 1;
	std::unique_ptr<wchar_t[]> cmdLine(new wchar_t[cmdLen]);
	if (FAILED(StringCchCopyW(cmdLine.get(), cmdLen, cmdRaw)))
	{
		throw gcnew System::Exception(gcnew System::String(WinApiHelpers::GetLastErrorMessage().c_str()));
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
	DWORD dwCreationFlags = NORMAL_PRIORITY_CLASS | CREATE_NEW_CONSOLE | CREATE_NEW_PROCESS_GROUP | CREATE_SUSPENDED;
	if (!additional.empty())
	{
		merged.reset(WinApiHelpers::CreateMergedEnvironmentBlock(additional));
		dwCreationFlags |= CREATE_UNICODE_ENVIRONMENT;
	}

	STARTUPINFOEXW si{ sizeof(si) };
	si.StartupInfo.wShowWindow = SW_SHOWDEFAULT;
	process_info_raii pi;

	std::wstring hookW(hookRaw);
	std::string hook = WinApiHelpers::WideToUtf8(hookW);
	BOOL success = WinApiHelpers::DetourCreateProcessWithDllExWrap(
		appPath,            // or retrieved from argv
		cmdLine.get(),      // command line (inherit)
		nullptr, nullptr,   // security attrs
		FALSE,              // inherit handles
		dwCreationFlags,
		merged.get(),       // Custom environment block
		nullptr,            // cwd
		&si.StartupInfo,
		(PROCESS_INFORMATION*) pi,
		hook.c_str(),       // *** injected DLL
		nullptr);           // default create-process routine

	std::wstring err = WinApiHelpers::GetLastErrorMessage();
	if (!success)
	{
		throw gcnew System::Exception(gcnew System::String(WinApiHelpers::GetLastErrorMessage().c_str()));
	}

	//success = DebugActiveProcess(pi.dwProcessId);
	success = ResumeThread(pi.pi.hThread);

	HandlePtr piHandle = WinApiHelpers::GetWindowsTerminalHandle(pi.pi.dwProcessId);
	if (piHandle.get() == nullptr)
	{
		throw gcnew System::Exception(gcnew System::String(WinApiHelpers::GetLastErrorMessage().c_str()));
	}
	//HandlePtr piHandle(pi.pi.hProcess);

	// Wait for the process to exit.
	WaitForSingleObject(piHandle.get(), INFINITE);

	merged.reset();
	cmdLine.reset();

	DWORD exitCode = 0;
	if (!GetExitCodeProcess(piHandle.get(), &exitCode))
	{
		throw gcnew System::Exception(gcnew System::String(WinApiHelpers::GetLastErrorMessage().c_str()));
	}

	if (exitCode != 0)
	{
		throw gcnew System::Exception(gcnew System::String(FormatProcessExitCode(exitCode).c_str()));
	}

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
int ProcessLauncher::LaunchProcessElevated(System::String^ launcherPath, System::String^ applicationPath, System::String^ commandLine, System::String^ envBlock, System::String^ hookPath)
{
	msclr::auto_handle<msclr::interop::marshal_context> ctx(gcnew msclr::interop::marshal_context());
	const wchar_t* _launcher = ctx->marshal_as<const wchar_t*>(launcherPath);
	const wchar_t* _appPath = ctx->marshal_as<const wchar_t*>(applicationPath);
	const wchar_t* _cmdLine = ctx->marshal_as<const wchar_t*>(commandLine);
	const wchar_t* _env = ctx->marshal_as<const wchar_t*>(envBlock);
	const wchar_t* _hook = ctx->marshal_as<const wchar_t*>(hookPath);

	std::wstring launcher(_launcher);
	std::wstring appPath(_appPath);
	std::wstring cmdLine(_cmdLine);
	std::wstring env(_env);
	std::wstring hook(_hook);

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

	std::wstring parameters = QuoteArgument(appPath) + L" " + QuoteArgument(cmdLine) + L" " + QuoteArgument(env) + L" " + QuoteArgument(hook);

	shellexecuteinfow_raii sei;
	sei.sei.cbSize = sizeof(sei);
	sei.sei.fMask = SEE_MASK_NOCLOSEPROCESS;
	sei.sei.lpVerb = L"runas"; // Request elevation (UAC prompt)
	sei.sei.lpFile = launcher.c_str();
	sei.sei.lpParameters = parameters.c_str();
	sei.sei.nShow = SW_HIDE;

	if (!ShellExecuteEx((SHELLEXECUTEINFOW*)sei))
	{
		throw gcnew System::Exception(gcnew System::String(WinApiHelpers::GetLastErrorMessage().c_str()));
	}

	// Wait for the launcher executable to complete.
	WaitForSingleObject(sei.sei.hProcess, INFINITE);

	DWORD exitCode = 0;
	if (!GetExitCodeProcess(sei.sei.hProcess, &exitCode))
	{
		throw gcnew System::Exception(gcnew System::String(WinApiHelpers::GetLastErrorMessage().c_str()));
	}

	if (exitCode == -1)
	{
		throw gcnew System::Exception(gcnew System::String(WinApiHelpers::GetLastErrorMessage().c_str()));
	}
	else if (exitCode != 0)
	{
		throw gcnew System::Exception(gcnew System::String(FormatProcessExitCode(exitCode).c_str()));
	}

	return static_cast<int>(exitCode);
}
