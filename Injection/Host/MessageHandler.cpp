#include "pch.h"
#include <vector>
#include "MessageHandler.h"
#include "InjectionHost.h"

class MessageRequest
{
public:
	MessageRequest(InjectedMessageType msgType, PVOID msg) noexcept
		: type{ msgType }
		, message{ reinterpret_cast<const Message*>(msg) }
	{
	}
	const InjectedMessageType type;
	const MessageVariant message;
	const MessageHeader& Header() const noexcept
	{
		return message.raw->header;
	}
};

static bool HandleCopyData(const CWPSTRUCT* pMsg)
{
	const HWND hwndSender{ reinterpret_cast<HWND>(pMsg->wParam) };
	const HWND hwndRecv{ pMsg->hwnd };
	const COPYDATASTRUCT* lpccds{ reinterpret_cast<COPYDATASTRUCT*>(pMsg->lParam) };
	const MessageRequest req{ static_cast<InjectedMessageType>(lpccds->dwData), lpccds->lpData };
	if (!req.Header().Valid())
		return false;

	COPYDATASTRUCT cds{};
	switch (req.type)
	{
	case InjectedMessageType::Discover:
	{
		DiscoverMessage reply;
		reply.header.hwnd = hwndRecv;
		reply.port = InjectionHost::GetInstance().Port();
		cds.dwData = static_cast<ULONG_PTR>(InjectedMessageType::Discover);
		cds.lpData = &reply;
		cds.cbData = sizeof(DiscoverMessage);
		SendMessage(hwndSender, WM_COPYDATA, reinterpret_cast<WPARAM>(hwndRecv), reinterpret_cast<LPARAM>(&cds));
		return true;
	}
	case InjectedMessageType::Ping:
	{
		PingMessage reply;
		reply.header.hwnd = hwndRecv;
		cds.dwData = static_cast<ULONG_PTR>(InjectedMessageType::Discover);
		cds.lpData = &reply;
		cds.cbData = sizeof(DiscoverMessage);
		SendMessage(hwndSender, WM_COPYDATA, reinterpret_cast<WPARAM>(hwndRecv), reinterpret_cast<LPARAM>(&cds));
		return true;
	}
	}

	return false;
}

extern "C"
__declspec(dllexport) LRESULT HandleHookedMessage(int code, WPARAM wParam, LPARAM lParam)
{
	const CWPSTRUCT* pMsg{ reinterpret_cast<CWPSTRUCT*>(lParam) };
	
	InjectionHost::GetInstance().ProcessMessages();

	if (pMsg)
	{
		switch (pMsg->message) {
		case WM_COPYDATA:
			if (HandleCopyData(pMsg))
				return CallNextHookEx(NULL, code, wParam, lParam);
			break;
		default:
			break;
		}
	}

	return CallNextHookEx(NULL, code, wParam, lParam);
}
