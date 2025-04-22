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

		/// <summary>
  		/// Provides utility methods for Windows API operations.
  		/// </summary>
  		/// <remarks>
  		/// Contains static methods for retrieving system error information and manipulating environment blocks.
  		/// </remarks>
		class WinApiHelpers
		{
		public:
			/// <summary>
			/// Retrieves the error message associated with the last Windows API error.
			/// </summary>
			/// <returns>A wide string containing the error message for the last system error.</returns>
			/// <remarks>
			/// This method uses the Windows API GetLastError() to obtain the current error code and FormatMessage() to convert it to a human-readable string.
			/// </remarks>
			WINAPIHELPERS_API static std::wstring GetLastErrorMessage();
			/// <summary>
   			/// Creates a merged environment block by combining existing environment variables with additional variables.
   			/// </summary>
   			/// <param name="additionalVars">A vector of wide strings containing additional environment variables to be added.</param>
   			/// <returns>A pointer to the newly created merged environment block (LPWSTR).</returns>
   			/// <remarks>
   			/// This method allows for dynamically creating an environment block with extra variables beyond the current process environment.
   			/// The caller is responsible for freeing the returned environment block using the appropriate Windows API function.
   			/// </remarks>
			WINAPIHELPERS_API static LPWSTR CreateMergedEnvironmentBlock(const std::vector<std::wstring>& additionalVars);
		};

	}
} // namespace WTLayoutManager::Services
