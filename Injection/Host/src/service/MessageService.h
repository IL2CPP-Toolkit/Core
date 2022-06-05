#pragma once
#include "Protocol.pb.h"
#include "Protocol.grpc.pb.h"

class ExecutionQueue;

class MessageServiceImpl : public messageService::MessageService::Service
{
public:
	MessageServiceImpl(ExecutionQueue& queue) noexcept;
	::grpc::Status SendMessage(::grpc::ServerContext* context, const ::messageService::MessageRequest* request, ::messageService::MessageReply* response) override;
private:
	ExecutionQueue& m_executionQueue;
};
