#pragma once
#include <stdint.h>

constexpr const char MessageToken[37]{ "35634bb5-ddc5-4f10-8b00-fd3806428ab5" };

enum class InjectedMessageType : uint8_t
{
	Ping = 1,
	Discover = 2,
};

struct MessageHeader
{
	MessageHeader() noexcept
	{
		strcpy_s(&token[0], sizeof(MessageToken), &MessageToken[0]);
	}
	char token[37];
	HWND hwnd{};

	bool Valid() const noexcept
	{
		return 0 == strncmp(&token[0], &MessageToken[0], sizeof(MessageToken));
	}
};

struct Message
{
	MessageHeader header;
};

struct PingMessage : Message {};

struct DiscoverMessage : Message
{
	uint32_t port;
};

union MessageVariant
{
	const Message* raw;
	const PingMessage* ping;
	const DiscoverMessage* discover;
};