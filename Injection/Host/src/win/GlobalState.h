#pragma once
#include <windows.h>
#include <SmartHandle.h>
#include <stdio.h>
#include <publicapi.h>

constexpr uint32_t HookedProcessLimit{64};

struct GameProcessHook
{
	DWORD pid{0};
	PublicState state;
	GameProcessHook() : pid{0} {}
	GameProcessHook(const GameProcessHook& other) : pid{other.pid}, state{other.state} {}
	GameProcessHook& operator=(const GameProcessHook& other) noexcept
	{
		pid = other.pid;
		state = other.state;
		return *this;
	}
};

struct GameProcessHookState
{
	DWORD sig;
	GameProcessHook pGPH[HookedProcessLimit];
};

constexpr const size_t HookedProcessStateBufferLen{sizeof(GameProcessHookState)};
constexpr TCHAR szLockName[]{TEXT("RTK_GameProcessHooks_RW")};
constexpr TCHAR szMMapName[]{TEXT("RTK_GameProcessHooks")}; //{TEXT("Global\\RTK_GameProcessHooks")};
constexpr DWORD GameProcessHookDataSignature = 8008135;


class GlobalState
{
public:
	GlobalState(HANDLE hLock, HANDLE hMapFile, GameProcessHookState* pState) noexcept;
	static GlobalState* Instance() noexcept;

	PublicState GetGameProcessState(DWORD pid) const noexcept;
	void SetGameProcessState(DWORD pid, const PublicState& state) noexcept;
	void ClearGameProcessState(DWORD pid) noexcept;
	std::vector<GameProcessHook> GetSnapshot() noexcept;

private:
	int FindGameProcessHook(DWORD pid) const noexcept;
	const SmartHandle m_shLock;
	const SmartHandle m_shMapFile;
	GameProcessHookState * const m_pState;
};
