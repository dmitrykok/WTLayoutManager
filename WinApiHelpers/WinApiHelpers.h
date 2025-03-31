#pragma once

#include "new.h"
#include <windows.h>
#include <string>
#include <vector>

#ifdef WINAPIHELPERS_EXPORTS   // Define this in your pure C++ DLL project settings
#define WINAPIHELPERS_API __declspec(dllexport)
#else
#define WINAPIHELPERS_API __declspec(dllimport)
#endif

namespace WTLayoutManager {
	namespace Services {

		class WinApiHelpers
		{
		public:
			WINAPIHELPERS_API static std::wstring GetLastErrorMessage();
			WINAPIHELPERS_API static LPWSTR CreateMergedEnvironmentBlock(const std::vector<std::wstring>& additionalVars);
		};

	}
} // namespace WTLayoutManager::Services
