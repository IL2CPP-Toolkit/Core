#include "pch.h"
#include "InjectionService.h"
#include "../InjectionHost.h"

InjectionServiceImpl::InjectionServiceImpl() noexcept {}

::grpc::Status InjectionServiceImpl::RegisterProcess(
	::grpc::ServerContext* context,
	const ::il2cppservice::RegisterProcessRequest* request,
	::il2cppservice::RegisterProcessResponse* response) noexcept
{
	InjectionHost::GetInstance()->RegisterProcess(request->pid());
	return ::grpc::Status::OK;
}

::grpc::Status InjectionServiceImpl::DeregisterProcess(
	::grpc::ServerContext* context,
	const ::il2cppservice::RegisterProcessRequest* request,
	::il2cppservice::RegisterProcessResponse* response) noexcept
{
	InjectionHost::GetInstance()->DeregisterProcess(request->pid());
	return ::grpc::Status::OK;
}

::grpc::Status InjectionServiceImpl::Detach(
	::grpc::ServerContext* context,
	const ::il2cppservice::DetachRequest* request,
	::il2cppservice::DetachResponse* response) noexcept
{
	InjectionHost::GetInstance()->Detach();
	return ::grpc::Status::OK;
}
