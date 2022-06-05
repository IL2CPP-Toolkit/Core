#pragma once

#include <stdint.h>
#include "il2cpp.h"
#include "il2cpp-config-api.h"
#include "imported_method.h"

static inline imported_module hGameAssembly{ "gameassembly.dll" };
#define DO_API(r, n, p) static inline imported_method<r p> n{ hGameAssembly, #n }
#define DO_API_NO_RETURN(r, n, p)   DO_API(r n p);
#include "il2cpp-api-functions.h"
#undef DO_API
#undef DO_API_NORETURN
