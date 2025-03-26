#include "pch.h"
#include "ProcessLauncherWrapper.h"
#include <windows.h>
#include <stdexcept>
#include <string>
#include <vector>
#include <msclr/marshal_cppstd.h>

using namespace msclr::interop;

using namespace WTLayoutManager::Services;

// Helper to retrieve the last error message.
static std::wstring GetLastErrorMessage() {
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
static LPWSTR CreateMergedEnvironmentBlock(const std::vector<std::wstring>& additionalVars)
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

int ProcessLauncher::LaunchProcess(System::String^ applicationPath, System::String^ commandLine, System::String^ envBlock)
{
    std::wstring appPath = marshal_as<std::wstring>(applicationPath);
    std::wstring cmdLine = L"\"" + marshal_as<std::wstring>(commandLine) + L"\"";
    std::wstring env = marshal_as<std::wstring>(envBlock);

    // Duplicate the environment block into a modifiable buffer.
	DWORD dwCreationFlags = 0;
    LPWSTR envCopy = NULL;
    if (env.size())
    {
        std::vector<std::wstring> additional = { env.c_str() };
        envCopy = CreateMergedEnvironmentBlock(additional);
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
        envCopy,                              // Custom environment block
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
        std::wstring err = GetLastErrorMessage();
        throw gcnew System::Exception(gcnew System::String(err.c_str()));
    }

    // Wait for the process to exit.
    WaitForSingleObject(pi.hProcess, INFINITE);

    DWORD exitCode = 0;
    if (!GetExitCodeProcess(pi.hProcess, &exitCode))
    {
        std::wstring err = GetLastErrorMessage();
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
    std::wstring cmdLine = marshal_as<std::wstring>(commandLine);
    std::wstring env = marshal_as<std::wstring>(envBlock);

    // Encode parameters as a sequence of quoted strings.
    // For the environment block, we assume the caller has replaced the embedded nulls
    // with semicolons (e.g. "VAR1=Value1;VAR2=Value2").
    std::wstring parameters = L"\"";
    parameters += appPath + L"\" \"";
    parameters += L"\\\"" + cmdLine + L"\\\"\" \"";
    parameters += env + L"\"";

    SHELLEXECUTEINFOW sei = { 0 };
    sei.cbSize = sizeof(sei);
    sei.fMask = SEE_MASK_NOCLOSEPROCESS;
    sei.lpVerb = L"runas"; // Request elevation (UAC prompt)
    sei.lpFile = launcher.c_str();
    sei.lpParameters = parameters.c_str();
    sei.nShow = SW_HIDE;

    if (!ShellExecuteEx(&sei))
    {
        std::wstring err = GetLastErrorMessage();
        throw gcnew System::Exception(gcnew System::String(err.c_str()));
    }

    // Wait for the launcher executable to complete.
    WaitForSingleObject(sei.hProcess, INFINITE);
    DWORD exitCode = 0;
    if (!GetExitCodeProcess(sei.hProcess, &exitCode))
    {
        std::wstring err = GetLastErrorMessage();
        CloseHandle(sei.hProcess);
        throw gcnew System::Exception(gcnew System::String(err.c_str()));
    }
        
    if (exitCode == -1)
	{
		// ShellExecuteEx failed.
		std::wstring err = GetLastErrorMessage();
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
