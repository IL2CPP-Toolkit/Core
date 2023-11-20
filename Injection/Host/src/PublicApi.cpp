#include "pch.h"
#include <unordered_map>
#include <memory>
#include <chrono>
#include <thread>
#include "SmartHandle.h"
#include "win/Snapshot.h"
#include "win/InjectionHook.h"
#include "win/WindowHelpers.h"
#include "win/GlobalState.h"
#include "MessageHandler.h"
#include "PublicApi.h"

#include <grpc/grpc.h>
#include <grpcpp/channel.h>
#include <grpcpp/client_context.h>
#include <grpcpp/create_channel.h>
#include "il2cpp.pb.h"
#include "il2cpp.grpc.pb.h"

std::unordered_map<DWORD, std::unique_ptr<HookHandle>> g_hookMap;

void CALLBACK HookProcess(HWND hWnd, HINSTANCE hInst, LPWSTR lpszCmdLine, int nCmdShow)
{
	if (lstrlenW(lpszCmdLine) == 0)
		return;
	int pid = std::stoi(lpszCmdLine);
	if (pid <= 0)
		return;

	InjectHook(pid);
}

void CALLBACK UnhookAll(HWND hWnd, HINSTANCE hInst, LPWSTR lpszCmdLine, int nCmdShow)
{
	GlobalState* pGlobal{GlobalState::Instance()};
	if (!pGlobal)
		return;

	std::vector<GameProcessHook> hooks = pGlobal->GetSnapshot();
	for (auto& hook : hooks)
	{
		std::string target{"localhost:"};
		target.append(std::to_string(hook.state.port));
		il2cppservice::InjectionService::Stub stub{::grpc::CreateChannel(target, grpc::InsecureChannelCredentials())};
		::grpc::ClientContext context;
		::il2cppservice::DetachRequest req;
		::il2cppservice::DetachResponse res;
		stub.Detach(&context, req, &res);
	}
}

extern "C" __declspec(dllexport) HRESULT WINAPI GetState(DWORD procId, PublicState* pState, long timeoutMs) noexcept
{
	HMODULE thisModule{};

	HRESULT result{E_NOINTERFACE};
	std::chrono::system_clock::time_point deadline{std::chrono::system_clock::now() + std::chrono::milliseconds{timeoutMs}};
	GlobalState* pGlobal{GlobalState::Instance()};
	if (!pGlobal)
		return E_NOINTERFACE;

	do
	{
		PublicState state{pGlobal->GetGameProcessState(procId)};
		if (state.port == -1)
			continue;

		// found:
		if (pState)
			*pState = state; // copy state

		return S_OK;
	} while (std::chrono::system_clock::now() < deadline);

	return ERROR_TIMEOUT;
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
	Snapshot snapshot{procId};
	if (!snapshot.FindProcess(procId) || !snapshot.FindFirstThread(procId))
		return E_INVALIDARG;

	g_hookMap.emplace(procId, injection.Hook(WH_CALLWNDPROC, snapshot.Thread().th32ThreadID));

	// bootstrap the hook with a ping WM_NULL message
	HWND hwndMain{GetMainWindowForProcessId(procId, L"UnityWndClass")};
	if (hwndMain)
		SendMessage(hwndMain, WM_NULL, 0, 0);

	HRESULT getStateResult{};
	do
	{
		PublicState state;
		getStateResult = GetState(procId, &state, 30000);
	} while (getStateResult != S_OK);

	return getStateResult;
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
