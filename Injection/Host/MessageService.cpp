#include "pch.h"
#include <string>
#include "gen\Protocol.pb.cc"
#include "gen\Protocol.grpc.pb.cc"
#include "MessageService.h"
#include "ExecutionQueue.h"

MessageServiceImpl::MessageServiceImpl(ExecutionQueue& queue) noexcept
	: m_executionQueue{ queue }
{
}

::grpc::Status MessageServiceImpl::SendMessage(::grpc::ServerContext* context, const ::rtk::MessageRequest* request, ::rtk::MessageReply* response)
{
	std::optional<::grpc::Status> result{ m_executionQueue.Invoke<::grpc::Status>([&]() mutable noexcept
	{
		std::string rsp{ request->msg() };
		rsp.append(" response!");
		response->set_reply(rsp);
		return ::grpc::Status::OK;
	}) };
	return result.value_or(::grpc::Status::CANCELLED);
}
