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

	::grpc::Status CallMethod(
		::grpc::ServerContext* context,
		const ::il2cppservice::CallMethodRequest* request,
		::il2cppservice::CallMethodResponse* response) noexcept override;

	::grpc::Status GetTypeInfo(
		::grpc::ServerContext* context,
		const ::il2cppservice::GetTypeInfoRequest* request,
		::il2cppservice::GetTypeInfoResponse* response) noexcept override;

	::grpc::Status PinObject(
		::grpc::ServerContext* context,
		const ::il2cppservice::PinObjectMessage* request,
		::il2cppservice::PinObjectMessage* response) noexcept override;

	::grpc::Status FreeObject(
		::grpc::ServerContext* context,
		const ::il2cppservice::FreeObjectRequest* request,
		::il2cppservice::FreeObjectResponse* response) noexcept override;

private:
	ExecutionQueue& m_executionQueue;
};
