#include "pch.h"
#include "MessageService.h"
#include "gen\Protocol.pb.cc"
#include "gen\Protocol.grpc.pb.cc"
#include <string>

::grpc::Status MessageServiceImpl::SendMessage(::grpc::ServerContext* context, const ::rtk::MessageRequest* request, ::rtk::MessageReply* response)
{
	std::string rsp{ request->msg() };
	rsp.append(" response!");
	response->set_reply(rsp);
	return ::grpc::Status::OK;
}
