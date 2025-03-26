#pragma once

#include <windows.h>
#include <string>
#include <vector>

namespace WTLayoutManager {
	namespace Services {

		class WinApiHelpers
		{
		public:
			__declspec(dllexport) static std::wstring GetLastErrorMessage();
			__declspec(dllexport) static LPWSTR CreateMergedEnvironmentBlock(const std::vector<std::wstring>& additionalVars);
		};

	}
} // namespace WTLayoutManager::Services
