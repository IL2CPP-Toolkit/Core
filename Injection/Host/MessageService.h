#pragma once
#include "gen\Protocol.pb.h"
#include "gen\Protocol.grpc.pb.h"

class MessageServiceImpl : public rtk::MessageService::Service
{
public:
	::grpc::Status SendMessage(::grpc::ServerContext* context, const ::rtk::MessageRequest* request, ::rtk::MessageReply* response) override;
};
