#pragma once
#include "il2cpp.pb.h"
#include "il2cpp.grpc.pb.h"
#include <map>
#include <string>

class ExecutionQueue;
struct Il2CppClass;
class Il2CppClassInfo;

class Il2CppServiceImpl : public il2cppservice::Il2CppService::Service
{
public:
	Il2CppServiceImpl(ExecutionQueue& queue) noexcept;

	::grpc::Status FindClass(
		::grpc::ServerContext* context, 
		const ::il2cppservice::FindClassRequest* request, 
		::il2cppservice::FindClassResponse* response
	) noexcept override;

	::grpc::Status CallMethod(
		::grpc::ServerContext* context, 
		const ::il2cppservice::CallMethodRequest* request, 
		::il2cppservice::CallMethodResponse* response
	) noexcept override;

private:
	ExecutionQueue& m_executionQueue;
};
