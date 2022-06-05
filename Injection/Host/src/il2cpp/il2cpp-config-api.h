#pragma once

#define IL2CPP_EXPORT __declspec(dllexport)
#define IL2CPP_IMPORT __declspec(dllimport)

#if IL2CPP_COMPILER_MSVC || defined(__ARMCC_VERSION)
#define NORETURN __declspec(noreturn)
#elif (IL2CPP_POINTER_SPARE_BITS == 0) && (defined(__clang__) || defined(__GNUC__))
#define NORETURN __attribute__ ((noreturn))
#else
#define NORETURN
#endif
