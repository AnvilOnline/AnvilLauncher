#include <Windows.h>
#include <string>
#include <atlbase.h>
#include <Shobjidl.h>

// https://blogs.msdn.microsoft.com/going_metro/2012/11/26/modern-app-automation/
HRESULT LaunchApp(const std::wstring& strAppUserModelId, PDWORD pdwProcessId)
{
	CComPtr<IApplicationActivationManager> spAppActivationManager;
	HRESULT hrResult = E_INVALIDARG;
	if (!strAppUserModelId.empty())
	{
		// Instantiate IApplicationActivationManager
		hrResult = CoCreateInstance(CLSID_ApplicationActivationManager,
			NULL,
			CLSCTX_LOCAL_SERVER,
			IID_IApplicationActivationManager,
			(LPVOID*)&spAppActivationManager);

		if (SUCCEEDED(hrResult))
		{
			// This call ensures that the app is launched as the foreground window
			hrResult = CoAllowSetForegroundWindow(spAppActivationManager, NULL);

			// Launch the app
			if (SUCCEEDED(hrResult))
			{
				hrResult = spAppActivationManager->ActivateApplication(strAppUserModelId.c_str(),
					NULL,
					AO_NONE,
					pdwProcessId);
			}
		}
	}

	return hrResult;
}

int main()
{
	auto s_PathName = L"Microsoft.Halo5Forge_8wekyb3d8bbwe!Ausar";
	auto s_Rekt = L"Microsoft.Halo5Forge_1.114.4592.2_x64__8wekyb3d8bbwe";

	auto s_Result = CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);
	if (s_Result != S_OK)
		return s_Result;

	CComQIPtr<IPackageDebugSettings> s_DebugSettings;
	s_Result = s_DebugSettings.CoCreateInstance(CLSID_PackageDebugSettings, nullptr, CLSCTX_ALL);
	if (s_Result != S_OK)
		return s_Result;

	s_Result = s_DebugSettings->EnableDebugging(s_Rekt, nullptr, nullptr);
	if (s_Result != S_OK)
		return s_Result;

	DWORD s_ProcessId = 0;
	s_Result = LaunchApp(s_PathName, &s_ProcessId);
	if (s_Result != S_OK)
		return s_Result;

	s_DebugSettings->Suspend(s_Rekt);

	CoUninitialize();

	return S_OK;
}