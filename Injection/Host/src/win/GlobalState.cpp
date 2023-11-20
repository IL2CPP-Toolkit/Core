#include "pch.h"
#include <mutex>
#include <vector>
#include "GlobalState.h"
#include <debug.h>
#include <sddl.h>

struct MutexLock
{
	MutexLock(HANDLE hMutex, DWORD dwTimeout) noexcept : hMutex{hMutex}, dwResult{WaitForSingleObject(hMutex, dwTimeout)} {}
	~MutexLock() noexcept
	{
		if (hMutex)
			ReleaseMutex(hMutex);
		hMutex = nullptr;
	}
	void Release() noexcept
	{
		if (hMutex)
			ReleaseMutex(hMutex);
		hMutex = nullptr;
	}
	HANDLE hMutex;
	DWORD dwResult;
};

GlobalState::GlobalState(HANDLE hLock, HANDLE hMapFile, GameProcessHookState* pState) noexcept
	: m_shLock{hLock}, m_shMapFile{hMapFile}, m_pState{pState}
{}

static GlobalState* CreateInstance() noexcept
{
	HANDLE hLock{CreateMutex(nullptr, FALSE, szLockName)};
	if (!hLock)
	{
		DWORD err{GetLastError()};
		DebugLog("Failed to obtain mutex (GetLastError()={})\n", err);

		return nullptr;
	}

	{
		MutexLock lock{hLock, INFINITE};
		HANDLE hMapFile{
			CreateFileMapping(
				INVALID_HANDLE_VALUE,        // use paging file
				NULL,                        // default security
				PAGE_READWRITE,              // read/write access
				0,                           // maximum object size (high-order DWORD)
				HookedProcessStateBufferLen, // maximum object size (low-order DWORD)
				szMMapName)                  // name of mapping object
		};
		if (!hMapFile)
		{
			DWORD err{GetLastError()};
			DebugLog("Failed to open/create mmap file (GetLastError()={})\n", err);

			lock.Release();
			CloseHandle(hLock);
			return nullptr;
		}

		void* pData{MapViewOfFile(hMapFile, FILE_MAP_ALL_ACCESS, 0, 0, HookedProcessStateBufferLen)};
		if (!pData)
		{
			DWORD err{GetLastError()};
			DebugLog("Failed open mmap view (GetLastError()={})\n", err);

			lock.Release();
			CloseHandle(hLock);
			return nullptr;
		}
		GameProcessHookState* pState{reinterpret_cast<GameProcessHookState*>(pData)};
		if (pState->sig != GameProcessHookDataSignature)
		{
			ZeroMemory(pState, sizeof(GameProcessHookState));
			pState->sig = GameProcessHookDataSignature;
		}
		return new GlobalState(hLock, hMapFile, pState);
	}
}

/*static*/ GlobalState* GlobalState::Instance() noexcept
{
	static GlobalState* pInstance{CreateInstance()};
	return pInstance;
}

int GlobalState::FindGameProcessHook(DWORD pid) const noexcept
{
	for (size_t n{0}; n < HookedProcessLimit; ++n)
	{
		if (m_pState->pGPH[n].pid == pid)
			return n;
	}
	return -1;
}

PublicState GlobalState::GetGameProcessState(DWORD pid) const noexcept
{
	MutexLock lock{m_shLock, INFINITE};
	if (lock.dwResult == WAIT_TIMEOUT)
		return {};

	int index{FindGameProcessHook(pid)};
	if (index == -1)
		return {};
	PublicState snapshot{m_pState->pGPH[index].state};
	return snapshot;
}

void GlobalState::SetGameProcessState(DWORD pid, const PublicState& state) noexcept
{
	MutexLock lock{m_shLock, INFINITE};
	int index{FindGameProcessHook(pid)};
	if (index == -1)
		index = FindGameProcessHook(0); // find next empty slot
	if (index == -1)
		return; // no empty slots
	m_pState->pGPH[index].pid = pid;
	m_pState->pGPH[index].state = state;
}

void GlobalState::ClearGameProcessState(DWORD pid) noexcept
{
	MutexLock lock{m_shLock, INFINITE};
	int index{FindGameProcessHook(pid)};
	if (index == -1)
		return;
	m_pState->pGPH[index].pid = 0;
	m_pState->pGPH[index].state = {};
}

std::vector<GameProcessHook> GlobalState::GetSnapshot() noexcept
{
	std::vector<GameProcessHook> results;
	for (size_t n{0}; n < HookedProcessLimit; ++n)
	{
		if (m_pState->pGPH[n].pid > 0)
			results.push_back(m_pState->pGPH[n]);
	}
	return results;
}
