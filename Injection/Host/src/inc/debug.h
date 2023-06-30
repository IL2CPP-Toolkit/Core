#pragma once
#include <string>
#include <format>

template<typename... Args>
static void DebugLog(std::string_view rt_fmt_str, Args&&... args)
{
	std::string line = std::vformat(rt_fmt_str, std::make_format_args(args...));
	OutputDebugStringA(line.c_str());
}
