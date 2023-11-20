#pragma once
#include "il2cpp.pb.h"
#include "il2cpp.grpc.pb.h"
#include <map>
#include <string>

class InjectionServiceImpl : public il2cppservice::InjectionService::Service
{
public:
	InjectionServiceImpl() noexcept;

	::grpc::Status Detach(
		::grpc::ServerContext* context,
		const ::il2cppservice::DetachRequest* request,
		::il2cppservice::DetachResponse* response) noexcept override;
};
