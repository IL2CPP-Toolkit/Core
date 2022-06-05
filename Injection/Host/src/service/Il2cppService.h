#pragma once
#include "il2cpp.pb.h"
#include "il2cpp.grpc.pb.h"

class ExecutionQueue;

class Il2CppServiceImpl : public il2cppservice::Il2CppService::Service
{
public:
	Il2CppServiceImpl(ExecutionQueue& queue) noexcept;
	::grpc::Status FindClass(::grpc::ServerContext* context, const ::il2cppservice::FindClassRequest* request, ::il2cppservice::FindClassResponse* response) override;
private:
	ExecutionQueue& m_executionQueue;
};
