// WTLocalStateHook.cpp — Windows Terminal LocalState redirection hook
// Build: cl /std:c++17 /MD /O2 /LD WTLocalStateHook.cpp detours.lib shlwapi.lib
// Requires: Detours 4.x public headers & libs
// Usage: 1) set env var WT_REDIRECT_LOCALSTATE to your desired profile root
//        2) DetourCreateProcessWithDllExW(..., L"...\\WTLocalStateHook.dll", …)

#include "pch.h"
#include <windows.h>
#include <shlobj.h>          // SHGetFolderPathW
#include <winternl.h>        // NtCreateFile, UNICODE_STRING
#include <detours.h>
#include <string>
#include <vector>

#pragma comment(lib, "ntdll.lib")
#pragma comment(lib, "shell32.lib")

//--------------------------------------------------------------------------
// Globals
//--------------------------------------------------------------------------
static std::wstring g_defaultPrefix;   // canonical LocalState path
static std::wstring g_newPrefix;       // replacement root (profile)

// Original function pointers ------------------------------------------------
static auto Real_CreateFileW = ::CreateFileW;
using PFN_NtCreateFile = NTSTATUS(NTAPI*)(
    PHANDLE, ACCESS_MASK, POBJECT_ATTRIBUTES, PIO_STATUS_BLOCK,
    PLARGE_INTEGER, ULONG, ULONG, ULONG, ULONG, PVOID, ULONG);
static PFN_NtCreateFile Real_NtCreateFile = nullptr;

//--------------------------------------------------------------------------
// Helpers
//--------------------------------------------------------------------------
static void InitPrefixes()
{
    if (!g_defaultPrefix.empty()) return;          // already cached

    // 1. default LocalState — build it at runtime so the hook works for any user
    wchar_t localAppData[MAX_PATH];
    if (SUCCEEDED(SHGetFolderPathW(nullptr, CSIDL_LOCAL_APPDATA, nullptr, SHGFP_TYPE_CURRENT, localAppData)))
    {
        g_defaultPrefix.assign(localAppData);
        g_defaultPrefix.append(L"\\Packages\\WindowsTerminalDev_6q6wn7rc29ae4\\LocalState");
    }

    // 2. new LocalState root — read once from env var
    wchar_t buf[MAX_PATH];
    DWORD len = GetEnvironmentVariableW(L"WT_REDIRECT_LOCALSTATE", buf, MAX_PATH);
    if (len > 0 && len < MAX_PATH)
        g_newPrefix.assign(buf, len);
}

// Replace beginning of |path| if it starts with the canonical LocalState root
static std::wstring RewritePath(const std::wstring& path)
{
    if (g_newPrefix.empty()) return path;

    if (path.rfind(g_defaultPrefix, 0) == 0)      // prefix‑match at pos 0
    {
        std::wstring rewritten = g_newPrefix;
        rewritten.append(path.substr(g_defaultPrefix.length()));
        return rewritten;
    }
    return path;
}

//--------------------------------------------------------------------------
// Hooked APIs
//--------------------------------------------------------------------------
static HANDLE WINAPI Hook_CreateFileW(LPCWSTR lpFileName,
    DWORD dwDesiredAccess,
    DWORD dwShareMode,
    LPSECURITY_ATTRIBUTES lpSecurityAttributes,
    DWORD dwCreationDisposition,
    DWORD dwFlagsAndAttributes,
    HANDLE hTemplateFile)
{
    InitPrefixes();
    std::wstring newName = lpFileName ? RewritePath(lpFileName) : std::wstring();
    return Real_CreateFileW(newName.empty() ? lpFileName : newName.c_str(),
        dwDesiredAccess, dwShareMode, lpSecurityAttributes,
        dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
}

static NTSTATUS NTAPI Hook_NtCreateFile(PHANDLE            FileHandle,
    ACCESS_MASK        DesiredAccess,
    POBJECT_ATTRIBUTES ObjectAttributes,
    PIO_STATUS_BLOCK   IoStatusBlock,
    PLARGE_INTEGER     AllocationSize,
    ULONG              FileAttributes,
    ULONG              ShareAccess,
    ULONG              CreateDisposition,
    ULONG              CreateOptions,
    PVOID              EaBuffer,
    ULONG              EaLength)
{
    InitPrefixes();

    UNICODE_STRING localCopy{};               // buffer lives on our stack
    OBJECT_ATTRIBUTES oaCopy = *ObjectAttributes; // shallow copy

    if (ObjectAttributes && ObjectAttributes->ObjectName && ObjectAttributes->ObjectName->Buffer)
    {
        std::wstring original(ObjectAttributes->ObjectName->Buffer,
            ObjectAttributes->ObjectName->Length / sizeof(WCHAR));
        std::wstring rewritten = RewritePath(original);
        if (rewritten != original)
        {
            RtlInitUnicodeString(&localCopy, rewritten.c_str());
            oaCopy.ObjectName = &localCopy;   // point to rewritten path
            ObjectAttributes = &oaCopy;       // use our modified copy
        }
    }

    return Real_NtCreateFile(FileHandle, DesiredAccess, ObjectAttributes, IoStatusBlock,
        AllocationSize, FileAttributes, ShareAccess,
        CreateDisposition, CreateOptions, EaBuffer, EaLength);
}

//--------------------------------------------------------------------------
// Detour attach / detach
//--------------------------------------------------------------------------
static void AttachDetours()
{
    DetourTransactionBegin();
    DetourUpdateThread(GetCurrentThread());

    DetourAttach(&(PVOID&)Real_CreateFileW, Hook_CreateFileW);

    if (!Real_NtCreateFile)
    {
        Real_NtCreateFile = reinterpret_cast<PFN_NtCreateFile>(
            GetProcAddress(GetModuleHandleW(L"ntdll.dll"), "NtCreateFile"));
    }
    if (Real_NtCreateFile)
        DetourAttach(&(PVOID&)Real_NtCreateFile, Hook_NtCreateFile);

    DetourTransactionCommit();
}

static void DetachDetours()
{
    DetourTransactionBegin();
    DetourUpdateThread(GetCurrentThread());
    DetourDetach(&(PVOID&)Real_CreateFileW, Hook_CreateFileW);
    if (Real_NtCreateFile)
        DetourDetach(&(PVOID&)Real_NtCreateFile, Hook_NtCreateFile);
    DetourTransactionCommit();
}

//--------------------------------------------------------------------------
// Mandatory export for Detours helper process
//--------------------------------------------------------------------------
//extern "C" __declspec(dllexport) void __cdecl DetourFinishHelperProcess() {}

//--------------------------------------------------------------------------
// DllMain
//--------------------------------------------------------------------------
BOOL APIENTRY DllMain(HMODULE hModule, DWORD reason, LPVOID)
{
    if (DetourIsHelperProcess()) return TRUE;   // skip in helper

    switch (reason)
    {
    case DLL_PROCESS_ATTACH:
        AttachDetours();
        break;
    case DLL_PROCESS_DETACH:
        DetachDetours();
        break;
    }
    return TRUE;
}
