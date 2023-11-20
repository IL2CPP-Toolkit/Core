#include "pch.h"
#include "InjectionService.h"
#include "../InjectionHost.h"

InjectionServiceImpl::InjectionServiceImpl() noexcept {}

::grpc::Status InjectionServiceImpl::Detach(
	::grpc::ServerContext* context,
	const ::il2cppservice::DetachRequest* request,
	::il2cppservice::DetachResponse* response) noexcept
{
	InjectionHost::GetInstance()->Detach();
	return ::grpc::Status::OK;
}
