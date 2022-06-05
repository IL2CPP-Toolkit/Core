#include "pch.h"
#include <string>
#include "il2cpp.pb.cc"
#include "il2cpp.grpc.pb.cc"
#include "Il2CppService.h"
#include "ExecutionQueue.h"
#include "../il2cpp/il2cpp-api.h"

Il2CppServiceImpl::Il2CppServiceImpl(ExecutionQueue& queue) noexcept
	: m_executionQueue{ queue }
{
}

::grpc::Status Il2CppServiceImpl::FindClass(::grpc::ServerContext* context, const ::il2cppservice::FindClassRequest* request, ::il2cppservice::FindClassResponse* response)
{
	std::optional<::grpc::Status> result{ m_executionQueue.Invoke<::grpc::Status>([&]() mutable noexcept
	{
		Il2CppDomain* pAppDomain{ il2cpp_domain_get() };
		size_t casm{};
		const Il2CppAssembly** ppAssemblies{ il2cpp_domain_get_assemblies(pAppDomain, &casm) };

		for (size_t n{ 0 }; n < casm; ++n) {
			const Il2CppAssembly* pAssembly{ *(ppAssemblies++) };
			const Il2CppImage* pImage{ il2cpp_assembly_get_image(pAssembly) };
			size_t cclass{ il2cpp_image_get_class_count(pImage) };
			for (size_t iClass{ 0 }; iClass < cclass; ++iClass) {
				const Il2CppClass* pClass{ il2cpp_image_get_class(pImage, iClass) };
				if (_strcmpi(pClass->_1.namespaze, request->namespaze().c_str()) == 0 && _strcmpi(pClass->_1.name, request->name().c_str()) == 0)
				{
					response->set_address(reinterpret_cast<uint64_t>(pClass));
					return ::grpc::Status::OK;
				}
			}
		}
		return ::grpc::Status::CANCELLED;
	}) };
	return result.value_or(::grpc::Status::CANCELLED);
}
