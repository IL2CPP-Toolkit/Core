#pragma once
#include "gen\Protocol.pb.h"
#include "gen\Protocol.grpc.pb.h"

class ExecutionQueue;

class MessageServiceImpl : public rtk::MessageService::Service
{
public:
	MessageServiceImpl(ExecutionQueue& queue) noexcept;
	::grpc::Status SendMessage(::grpc::ServerContext* context, const ::rtk::MessageRequest* request, ::rtk::MessageReply* response) override;
private:
	ExecutionQueue& m_executionQueue;
};
