#include "pch.h"
#include <string>
#include "ExecutionQueue.h"
#include <il2cpp/il2cpp-api.h>
#include "../il2cpp/Il2CppClassInfo.h"
#include "../il2cpp/Il2CppContext.h"
#include "../il2cpp/SystemString.h"
#include "il2cpp.pb.cc"
#include "il2cpp.grpc.pb.cc"
#include "Il2CppService.h"
#include <il2cpp/il2cpp-tabledefs.h>

Il2CppServiceImpl::Il2CppServiceImpl(ExecutionQueue& queue) noexcept
	: m_executionQueue{ queue }
{
}

struct numeric_value
{
	numeric_value(double val) noexcept { value.double_ = val; }
	numeric_value(float val) noexcept { value.float_ = val; }
	numeric_value(int32_t val) noexcept { value.int32_ = val; }
	numeric_value(int64_t val) noexcept { value.int64_ = val; }
	numeric_value(uint32_t val) noexcept { value.uint32_ = val; }
	numeric_value(uint64_t val) noexcept { value.uint64_ = val; }
	numeric_value(bool val) noexcept { value.byte_ = val; }
	numeric_value(byte val) noexcept { value.byte_ = val; }
	union {
		double double_;
		float float_;
		int32_t int32_;
		uint32_t uint32_;
		int64_t int64_;
		uint64_t uint64_;
		byte byte_;
	} value;
};

::grpc::Status Il2CppServiceImpl::CallMethod(
	::grpc::ServerContext* context,
	const ::il2cppservice::CallMethodRequest* request,
	::il2cppservice::CallMethodResponse* response) noexcept
{
	std::optional<::grpc::Status> result{ m_executionQueue.Invoke<::grpc::Status>(
		[&]() mutable noexcept
		{
			const Il2CppClassInfo* pClsInfo{ Il2CppContext::instance().FindClass(request->klass().name()) };
			if (!pClsInfo)
				return ::grpc::Status{ ::grpc::StatusCode::FAILED_PRECONDITION, "Object is not well formed" };

			Il2CppObject* pObj{ nullptr };
			if (request->has_instanceaddress())
			{
				if (pObj = reinterpret_cast<Il2CppObject*>(request->instanceaddress()))
				{
					if (pObj->klass != pClsInfo->klass())
						return ::grpc::Status{ ::grpc::StatusCode::FAILED_PRECONDITION, "Object is not well formed" };
				}
			}

			const int nArgs{ request->arguments_size() };
			const MethodInfo* pMethod{ il2cpp_class_get_method_from_name(pClsInfo->klass(), request->methodname().c_str(), nArgs) };
			if (!pMethod)
				return ::grpc::Status{ ::grpc::StatusCode::NOT_FOUND, "Method not found" };

			void** pArgs{ reinterpret_cast<void**>(il2cpp_alloc(sizeof(void*) * nArgs)) };
			if (!pArgs)
				return ::grpc::Status{ ::grpc::StatusCode::RESOURCE_EXHAUSTED, "Out of memory" };

			std::vector<numeric_value> numericArgs{};
			std::vector<SystemString> stringArgs{};
			for (int n{ 0 }, m{ request->arguments_size() }; n < m; ++n)
			{
				const ::il2cppservice::Value& arg { request->arguments().at(n) };
				if (arg.has_bit_())
				{
					numericArgs.emplace_back(arg.bit_());
					pArgs[n] = reinterpret_cast<void*>(&numericArgs.back());
				}
				else if (arg.has_double_())
				{
					numericArgs.emplace_back(arg.double_());
					pArgs[n] = reinterpret_cast<void*>(&numericArgs.back());
				}
				else if (arg.has_float_())
				{
					numericArgs.emplace_back(static_cast<float>(arg.float_()));
					pArgs[n] = reinterpret_cast<void*>(&numericArgs.back());
				}
				else if (arg.has_int32_())
				{
					numericArgs.emplace_back(arg.int32_());
					pArgs[n] = reinterpret_cast<void*>(&numericArgs.back());
				}
				else if (arg.has_uint32_())
				{
					numericArgs.emplace_back(arg.uint32_());
					pArgs[n] = reinterpret_cast<void*>(&numericArgs.back());
				}
				else if (arg.has_int64_())
				{
					numericArgs.emplace_back(arg.int64_());
					pArgs[n] = reinterpret_cast<void*>(&numericArgs.back());
				}
				else if (arg.has_uint64_())
				{
					numericArgs.emplace_back(arg.uint64_());
					pArgs[n] = reinterpret_cast<void*>(&numericArgs.back());
				}
				else if (arg.has_str_())
				{
					stringArgs.emplace_back(arg.str_());
					pArgs[n] = reinterpret_cast<void*>(&stringArgs.back());
				}
			}

			Il2CppException* pex;
			Il2CppObject* pResult{ il2cpp_runtime_invoke(pMethod, pObj, pArgs, &pex) };

			response->mutable_returnvalue()->set_address(reinterpret_cast<uint64_t>(pResult));
			if (pResult)
			{
				const Il2CppClassInfo resultKlass{ pResult->klass };
				response->mutable_returnvalue()->mutable_klass()->set_name(resultKlass.name());
			}

			il2cpp_free(pArgs);
			return ::grpc::Status::OK;
		})
	};
	return result.value_or(::grpc::Status::CANCELLED);
}

::grpc::Status Il2CppServiceImpl::GetTypeInfo(
	::grpc::ServerContext* context,
	const ::il2cppservice::GetTypeInfoRequest* request,
	::il2cppservice::GetTypeInfoResponse* response
) noexcept
{
	std::optional<::grpc::Status> result{ m_executionQueue.Invoke<::grpc::Status>([&]() mutable noexcept
	{
		const Il2CppClassInfo* pClassInfo{ Il2CppContext::instance().FindClass(request->klass().namespaze(), request->klass().name()) };
		if (!pClassInfo)
			return ::grpc::Status{ grpc::StatusCode::NOT_FOUND, "Could not find class" };

		const Il2CppClass* pCls{ pClassInfo->klass() };
		response->mutable_typeinfo()->set_address(reinterpret_cast<uint64_t>(pCls));
		response->mutable_typeinfo()->set_staticfieldsaddress(reinterpret_cast<uint64_t>(il2cpp_class_get_static_field_data(pCls)));
		response->mutable_typeinfo()->mutable_klassid()->set_name(pClassInfo->name());
		for (int n{ 0 }, m{ pCls->field_count }; n < m; ++n)
		{
			::il2cppservice::Il2CppField* pFld{ response->mutable_typeinfo()->mutable_fields()->Add() };
			pFld->set_name(pCls->fields[n].name);
			pFld->set_offset(pCls->fields[n].offset);
			pFld->set_static_(pCls->fields[n].type->attrs & FIELD_ATTRIBUTE_STATIC);
		}
		return ::grpc::Status::OK;
	}) };
	return result.value_or(::grpc::Status::CANCELLED);
}

::grpc::Status Il2CppServiceImpl::FindClass(
	::grpc::ServerContext* context,
	const ::il2cppservice::FindClassRequest* request,
	::il2cppservice::FindClassResponse* response) noexcept
{
	std::optional<::grpc::Status> result{ m_executionQueue.Invoke<::grpc::Status>([&]() mutable noexcept
	{
		const Il2CppClassInfo* pClassInfo{ Il2CppContext::instance().FindClass(request->klass().namespaze(), request->klass().name()) };
		if (!pClassInfo)
			return ::grpc::Status{ grpc::StatusCode::NOT_FOUND, "Could not find class" };

		response->set_address(reinterpret_cast<uint64_t>(pClassInfo->klass()));
		return ::grpc::Status::OK;
	}) };
	return result.value_or(::grpc::Status::CANCELLED);
}
