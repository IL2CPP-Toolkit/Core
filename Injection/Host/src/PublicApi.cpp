#include "pch.h"
#include <unordered_map>
#include <memory>
#include <chrono>
#include <thread>
#include "SmartHandle.h"
#include "win/Snapshot.h"
#include "win/InjectionHook.h"
#include "win/WindowHelpers.h"
#include "MessageHandler.h"
#include "PublicApi.h"

std::unordered_map<DWORD, std::unique_ptr<HookHandle>> g_hookMap;

/*static*/ PublicState PublicState::value{0};

extern "C" __declspec(dllexport) HRESULT WINAPI GetState(DWORD procId, PublicState* pState, long timeoutMs) noexcept
{
	HMODULE thisModule{};

	BOOL getHandleResult{GetModuleHandleEx(
		GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
		static_cast<LPCWSTR>(static_cast<void*>(&InjectHook)),
		&thisModule)};
	if (getHandleResult == 0)
		return E_NOINTERFACE;

	wchar_t wzModuleName[MAX_PATH + 1];
	wzModuleName[0] = 0;
	if (FAILED(GetModuleFileName(thisModule, &wzModuleName[0], MAX_PATH)))
		return E_NOINTERFACE;

	Snapshot snapshot;
	if (!snapshot.FindProcess(procId) || !snapshot.FindModule(wzModuleName))
		return E_NOINTERFACE;

	const byte* baseAddr{snapshot.Module().modBaseAddr};
	const size_t stateOffset{reinterpret_cast<size_t>(&PublicState::value) - reinterpret_cast<size_t>(thisModule)};
	const byte* remoteAddr{baseAddr + stateOffset};
	SmartHandle hProcess{OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, FALSE, procId)};
	size_t readBytes{};

	// scan for a port number until timeoutMs
	std::chrono::system_clock::time_point deadline{std::chrono::system_clock::now() + std::chrono::milliseconds{timeoutMs}};
	int count{0};
	bool lastResult{false};
	while (std::chrono::system_clock::now() < deadline && pState->port <= 0 && sizeof(PublicState) == readBytes)
	{
		++count;
		lastResult = ReadProcessMemory(hProcess, remoteAddr, pState, sizeof(PublicState), &readBytes);
		std::this_thread::sleep_for(std::chrono::milliseconds{10});
	}

	if (!lastResult)
	{
		DWORD err{GetLastError()};
		return err != 0 ? err : ERROR_TIMEOUT;
	}

	return S_OK;
}

extern "C" __declspec(dllexport) HRESULT WINAPI InjectHook(DWORD procId) noexcept
{
	if (g_hookMap.find(procId) != g_hookMap.cend())
		return E_ILLEGAL_STATE_CHANGE;

	HMODULE thisModule{};
	if (GetModuleHandleEx(
			GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
			static_cast<LPCWSTR>(static_cast<void*>(&InjectHook)),
			&thisModule)
		== 0)
	{
		return E_NOINTERFACE;
	}
	InjectionHook injection{thisModule, &HandleHookedMessage};
	Snapshot snapshot;
	if (!snapshot.FindProcess(procId) || !snapshot.FindFirstThread())
		return E_INVALIDARG;

	g_hookMap.emplace(procId, injection.Hook(WH_CALLWNDPROC, snapshot.Thread().th32ThreadID));

	// bootstrap the hook with a ping WM_NULL message
	HWND hwndMain{GetMainWindowForProcessId(procId, L"UnityWndClass")};
	if (hwndMain)
		SendMessage(hwndMain, WM_NULL, 0, 0);

	return S_OK;
}

extern "C" __declspec(dllexport) HRESULT WINAPI ReleaseHook(DWORD procId) noexcept
{
	const size_t numRemoved{g_hookMap.erase(procId)};
	if (numRemoved == 0)
		return E_ILLEGAL_STATE_CHANGE;

	return S_OK;
}

extern "C" __declspec(dllexport) InjectResult WINAPI GetHookState(DWORD procId) noexcept
{
	if (g_hookMap.find(procId) != g_hookMap.cend())
		return InjectResult::Hooked;

	return InjectResult::Unhooked;
}
