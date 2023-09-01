#include "pch.h"
#include <string>
#include <locale>
#include <codecvt>
#include <ranges>
#include <stack>
#include "ExecutionQueue.h"
#include <il2cpp/il2cpp-api.h>
#include <il2cpp/il2cpp-string-types.h>
#include <il2cpp/il2cpp-tabledefs.h>
#include "../il2cpp/Il2CppClassInfo.h"
#include "../il2cpp/Il2CppContext.h"
#include "../il2cpp/Il2CppHelpers.h"
#include "../il2cpp/SystemString.h"
#include "il2cpp.pb.cc"
#include "il2cpp.grpc.pb.cc"
#include "Il2CppService.h"

Il2CppServiceImpl::Il2CppServiceImpl(ExecutionQueue& queue) noexcept : m_executionQueue{queue} {}

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
	union
	{
		double double_;
		float float_;
		int32_t int32_;
		uint32_t uint32_;
		int64_t int64_;
		uint64_t uint64_;
		byte byte_;
	} value;
};

void SetClassId(::il2cppservice::ClassId* pClassId, const Il2CppClass* pClass) noexcept
{
	pClassId->set_name(il2cpp_class_get_name(const_cast<Il2CppClass*>(pClass)));
	pClassId->set_namespaze(il2cpp_class_get_namespace(const_cast<Il2CppClass*>(pClass)));
	pClassId->set_address(reinterpret_cast<uint64_t>(pClass));
	pClassId->set_isvaluetype(il2cpp_class_is_valuetype(pClass));
	const Il2CppClass* pDeclaringType = pClass;
	::il2cppservice::ClassId* pCurrentClass = pClassId;
	while ((pDeclaringType = pDeclaringType->declaringType) != nullptr)
	{
		pCurrentClass = pCurrentClass->mutable_declaringtype();
		SetClassId(pCurrentClass, pDeclaringType);
	}
}

static void ObjectToValue(Il2CppObject* pObj, const Il2CppType& cppType, ::il2cppservice::Value& value, bool pin = false) noexcept
{
	switch (cppType.type)
	{
		case Il2CppTypeEnum::IL2CPP_TYPE_BOOLEAN: {
			const bool* pValue{static_cast<bool*>(il2cpp_object_unbox(pObj))};
			value.set_bit_(*pValue);
			break;
		}
		case Il2CppTypeEnum::IL2CPP_TYPE_I1:
			__fallthrough;
		case Il2CppTypeEnum::IL2CPP_TYPE_CHAR: {
			const char* pValue{static_cast<char*>(il2cpp_object_unbox(pObj))};
			value.set_int32_(*pValue);
			break;
		}
		case Il2CppTypeEnum::IL2CPP_TYPE_U1: {
			const byte* pValue{static_cast<byte*>(il2cpp_object_unbox(pObj))};
			value.set_uint32_(*pValue);
			break;
		}
		case Il2CppTypeEnum::IL2CPP_TYPE_I2: {
			const int16_t* pValue{static_cast<int16_t*>(il2cpp_object_unbox(pObj))};
			value.set_int32_(*pValue);
			break;
		}
		case Il2CppTypeEnum::IL2CPP_TYPE_U2: {
			const uint16_t* pValue{static_cast<uint16_t*>(il2cpp_object_unbox(pObj))};
			value.set_uint32_(*pValue);
			break;
		}
		case Il2CppTypeEnum::IL2CPP_TYPE_I4: {
			const int32_t* pValue{static_cast<int32_t*>(il2cpp_object_unbox(pObj))};
			value.set_int32_(*pValue);
			break;
		}
		case Il2CppTypeEnum::IL2CPP_TYPE_U4: {
			const uint32_t* pValue{static_cast<uint32_t*>(il2cpp_object_unbox(pObj))};
			value.set_uint32_(*pValue);
			break;
		}
		case Il2CppTypeEnum::IL2CPP_TYPE_I8: {
			const int64_t* pValue{static_cast<int64_t*>(il2cpp_object_unbox(pObj))};
			value.set_int64_(*pValue);
			break;
		}
		case Il2CppTypeEnum::IL2CPP_TYPE_U8: {
			const uint64_t* pValue{static_cast<uint64_t*>(il2cpp_object_unbox(pObj))};
			value.set_uint64_(*pValue);
			break;
		}
		case Il2CppTypeEnum::IL2CPP_TYPE_R4: {
			const float* pValue{static_cast<float*>(il2cpp_object_unbox(pObj))};
			value.set_float_(*pValue);
			break;
		}
		case Il2CppTypeEnum::IL2CPP_TYPE_R8: {
			const double* pValue{static_cast<double*>(il2cpp_object_unbox(pObj))};
			value.set_double_(*pValue);
			break;
		}
		case Il2CppTypeEnum::IL2CPP_TYPE_CLASS: {
			const auto& pReturnObj = value.mutable_obj_();
			pReturnObj->set_address(reinterpret_cast<uint64_t>(pObj));
			if (pin)
			{
				uint32_t handle{il2cpp_gchandle_new(pObj, true)};
				pReturnObj->set_handle(handle);
			}
			SetClassId(pReturnObj->mutable_klass(), pObj->klass);
			break;
		}
		case Il2CppTypeEnum::IL2CPP_TYPE_STRING: {
			static std::wstring_convert<std::codecvt_utf8_utf16<wchar_t>> converter;

			Il2CppString* pStr{reinterpret_cast<Il2CppString*>(pObj)};
			UTF16String wzValue{&pStr->chars[0], static_cast<size_t>(pStr->length)};
			value.set_str_(converter.to_bytes(wzValue));
			break;
		}
		case Il2CppTypeEnum::IL2CPP_TYPE_VOID: {
			break;
		}
	}
}


template<typename T>
struct ArgumentValue
{
	ArgumentValue(T objValue, bool hasValue) : value{objValue}, has_value{hasValue} {}
	T value;
	bool has_value;
};

struct ArgumentValueHolder
{
	char unused[512];

	template<typename T>
	ArgumentValueHolder(T value, ::il2cppservice::NullableState nullableState)
	{
		ArgumentValue<T> argValue{value, nullableState == ::il2cppservice::NullableState::IsNull};
		memcpy_s(&unused[0], 512, &argValue, sizeof(ArgumentValue<T>));
	}
};

::grpc::Status Il2CppServiceImpl::CreateObject(
	::grpc::ServerContext* context,
	const ::il2cppservice::CreateObjectRequest* request,
	::il2cppservice::CreateObjectResponse* response) noexcept
{
	std::optional<::grpc::Status> result{m_executionQueue.Invoke<::grpc::Status>([&]() mutable {
		const Il2CppClassInfo* pClsInfo{Il2CppContext::instance().FindClass(request->klass().namespaze(), request->klass().name())};
		if (!pClsInfo)
			return ::grpc::Status{::grpc::StatusCode::FAILED_PRECONDITION, "Class not found"};
		Il2CppClass* pClass{pClsInfo->klass()};

		if (!pClass)
			return ::grpc::Status{::grpc::StatusCode::FAILED_PRECONDITION, "Class not found"};

		Il2CppObject* pObj{il2cpp_object_new(pClass)};

		const int nArgs{request->arguments_size()};
		const MethodInfo* pMethod{il2cpp_class_get_method_from_name(pClass, ".ctor", nArgs)};
		if (!pMethod)
			return ::grpc::Status{::grpc::StatusCode::NOT_FOUND, "Method not found"};

		void** pArgs{reinterpret_cast<void**>(il2cpp_alloc(sizeof(void*) * nArgs))};
		if (!pArgs)
			return ::grpc::Status{::grpc::StatusCode::RESOURCE_EXHAUSTED, "Out of memory"};

		try
		{
			std::vector<ArgumentValueHolder> argHolders{};
			for (int n{0}, m{request->arguments_size()}; n < m; ++n)
			{
				const ::il2cppservice::Value& arg{request->arguments().at(n)};
				if (arg.has_bit_())
				{
					ArgumentValueHolder& argHolder{argHolders.emplace_back(arg.bit_(), arg.nullstate())};
					pArgs[n] = reinterpret_cast<void*>(&argHolder);
				}
				else if (arg.has_double_())
				{
					ArgumentValueHolder& argHolder{argHolders.emplace_back(arg.double_(), arg.nullstate())};
					pArgs[n] = reinterpret_cast<void*>(&argHolder);
				}
				else if (arg.has_float_())
				{
					ArgumentValueHolder& argHolder{argHolders.emplace_back(static_cast<float>(arg.float_()), arg.nullstate())};
					pArgs[n] = reinterpret_cast<void*>(&argHolder);
				}
				else if (arg.has_int32_())
				{
					ArgumentValueHolder& argHolder{argHolders.emplace_back(arg.int32_(), arg.nullstate())};
					pArgs[n] = reinterpret_cast<void*>(&argHolder);
				}
				else if (arg.has_uint32_())
				{
					ArgumentValueHolder& argHolder{argHolders.emplace_back(arg.uint32_(), arg.nullstate())};
					pArgs[n] = reinterpret_cast<void*>(&argHolder);
				}
				else if (arg.has_int64_())
				{
					ArgumentValueHolder& argHolder{argHolders.emplace_back(arg.int64_(), arg.nullstate())};
					pArgs[n] = reinterpret_cast<void*>(&argHolder);
				}
				else if (arg.has_uint64_())
				{
					ArgumentValueHolder& argHolder{argHolders.emplace_back(arg.uint64_(), arg.nullstate())};
					pArgs[n] = reinterpret_cast<void*>(&argHolder);
				}
				else if (arg.has_str_())
				{
					ArgumentValueHolder& argHolder{argHolders.emplace_back(il2cpp_string_new(arg.str_().c_str()), arg.nullstate())};
					pArgs[n] = reinterpret_cast<void*>(&argHolder);
				}
				else if (arg.has_obj_())
				{
					Il2CppObject* pArg{il2cpp_object_from_ptr(reinterpret_cast<void*>(arg.obj_().address()))};
					// ArgumentValueHolder& argHolder{argHolders.emplace_back(pArg, arg.nullstate())};
					pArgs[n] = reinterpret_cast<void*>(pArg);
				}
			}

			Il2CppException* pEx{};
			Il2CppObject* pResult{il2cpp_runtime_invoke(pMethod, pObj, pArgs, &pEx)};
			if (pEx)
			{
				std::string exceptionMessage{il2cpp_format_exception_to_string(pEx) + ' ' + il2cpp_format_stack_trace_to_string(pEx)};
				return ::grpc::Status{::grpc::StatusCode::UNKNOWN, "Exception occurred"};
			}
			if (pObj)
			{
				::il2cppservice::Value* pRetVal{response->mutable_returnvalue()};
				ObjectToValue(pObj, *il2cpp_method_get_return_type(pMethod), *pRetVal, true);
			}
		}
		catch (...)
		{
			il2cpp_free(pArgs);
			return ::grpc::Status{::grpc::StatusCode::UNKNOWN, "Exception occurred"};
		}

		return ::grpc::Status::OK;
	})};
	return result.value_or(::grpc::Status::CANCELLED);
}

::grpc::Status Il2CppServiceImpl::CallMethod(
	::grpc::ServerContext* context,
	const ::il2cppservice::CallMethodRequest* request,
	::il2cppservice::CallMethodResponse* response) noexcept
{
	std::optional<::grpc::Status> result{m_executionQueue.Invoke<::grpc::Status>([&]() mutable {
		Il2CppObject* pObj{nullptr};
		Il2CppClass* pClass{nullptr};
		if (request->has_instance())
		{
			const ::il2cppservice::Il2CppObject& instance{request->instance()};
			if (!request->klass().isvaluetype())
			{
				pObj = il2cpp_object_from_ptr(reinterpret_cast<void*>(instance.address()));
				if (!pObj)
				{
					return ::grpc::Status{::grpc::StatusCode::FAILED_PRECONDITION, "Object not found"};
				}
				pClass = pObj->klass;
			}
			else
			{
				// value types aren't Il2CppObjects, but Il2Cpp treats them that way in all API contracts
				pObj = reinterpret_cast<Il2CppObject*>(instance.address());
			}
		}

		if (!pClass)
		{
			const Il2CppClassInfo* pClsInfo{Il2CppContext::instance().FindClass(request->klass().namespaze(), request->klass().name())};
			if (!pClsInfo)
				return ::grpc::Status{::grpc::StatusCode::FAILED_PRECONDITION, "Class not found"};
			pClass = pClsInfo->klass();
		}

		if (!pClass)
			return ::grpc::Status{::grpc::StatusCode::FAILED_PRECONDITION, "Class not found"};

		const int nArgs{request->arguments_size()};
		const MethodInfo* pMethod{il2cpp_class_get_method_from_name(pClass, request->methodname().c_str(), nArgs)};
		if (!pMethod)
			return ::grpc::Status{::grpc::StatusCode::NOT_FOUND, "Method not found"};

		if (il2cpp_method_get_flags(pMethod, /*iFlags*/ 0) & METHOD_ATTRIBUTE_STATIC)
		{
			assert(!pObj); // Method call does not expect an instance
		}
		else if (!pObj)
		{
			return ::grpc::Status{::grpc::StatusCode::INVALID_ARGUMENT, "Instance method requires a non-null instance"};
		}

		void** pArgs{reinterpret_cast<void**>(il2cpp_alloc(sizeof(void*) * nArgs))};
		if (!pArgs)
			return ::grpc::Status{::grpc::StatusCode::RESOURCE_EXHAUSTED, "Out of memory"};

		try
		{
			std::vector<ArgumentValueHolder> argHolders{};
			for (int n{0}, m{request->arguments_size()}; n < m; ++n)
			{
				const ::il2cppservice::Value& arg{request->arguments().at(n)};
				if (arg.has_bit_())
				{
					ArgumentValueHolder& argHolder{argHolders.emplace_back(arg.bit_(), arg.nullstate())};
					pArgs[n] = reinterpret_cast<void*>(&argHolder);
				}
				else if (arg.has_double_())
				{
					ArgumentValueHolder& argHolder{argHolders.emplace_back(arg.double_(), arg.nullstate())};
					pArgs[n] = reinterpret_cast<void*>(&argHolder);
				}
				else if (arg.has_float_())
				{
					ArgumentValueHolder& argHolder{argHolders.emplace_back(static_cast<float>(arg.float_()), arg.nullstate())};
					pArgs[n] = reinterpret_cast<void*>(&argHolder);
				}
				else if (arg.has_int32_())
				{
					ArgumentValueHolder& argHolder{argHolders.emplace_back(arg.int32_(), arg.nullstate())};
					pArgs[n] = reinterpret_cast<void*>(&argHolder);
				}
				else if (arg.has_uint32_())
				{
					ArgumentValueHolder& argHolder{argHolders.emplace_back(arg.uint32_(), arg.nullstate())};
					pArgs[n] = reinterpret_cast<void*>(&argHolder);
				}
				else if (arg.has_int64_())
				{
					ArgumentValueHolder& argHolder{argHolders.emplace_back(arg.int64_(), arg.nullstate())};
					pArgs[n] = reinterpret_cast<void*>(&argHolder);
				}
				else if (arg.has_uint64_())
				{
					ArgumentValueHolder& argHolder{argHolders.emplace_back(arg.uint64_(), arg.nullstate())};
					pArgs[n] = reinterpret_cast<void*>(&argHolder);
				}
				else if (arg.has_str_())
				{
					ArgumentValueHolder& argHolder{argHolders.emplace_back(il2cpp_string_new(arg.str_().c_str()), arg.nullstate())};
					pArgs[n] = reinterpret_cast<void*>(&argHolder);
				}
				else if (arg.has_obj_())
				{
					Il2CppObject* pArg{il2cpp_object_from_ptr(reinterpret_cast<void*>(arg.obj_().address()))};
					// ArgumentValueHolder& argHolder{argHolders.emplace_back(pArg, arg.nullstate())};
					pArgs[n] = reinterpret_cast<void*>(pArg);
				}
			}

			Il2CppException* pEx{};
			Il2CppObject* pResult{il2cpp_runtime_invoke(pMethod, pObj, pArgs, &pEx)};
			if (pEx)
			{
				std::string exceptionMessage{il2cpp_format_exception_to_string(pEx) + ' ' + il2cpp_format_stack_trace_to_string(pEx)};
				return ::grpc::Status{::grpc::StatusCode::UNKNOWN, "Exception occurred"};
			}
			if (pResult)
			{
				::il2cppservice::Value* pRetVal{response->mutable_returnvalue()};
				ObjectToValue(pResult, *il2cpp_method_get_return_type(pMethod), *pRetVal);
			}
		}
		catch (...)
		{
			il2cpp_free(pArgs);
			return ::grpc::Status{::grpc::StatusCode::UNKNOWN, "Exception occurred"};
		}
		return ::grpc::Status::OK;
	})};
	return result.value_or(::grpc::Status::CANCELLED);
}

::grpc::Status Il2CppServiceImpl::GetTypeInfo(
	::grpc::ServerContext* context,
	const ::il2cppservice::GetTypeInfoRequest* request,
	::il2cppservice::GetTypeInfoResponse* response) noexcept
{
	std::optional<::grpc::Status> result{m_executionQueue.Invoke<::grpc::Status>([&]() mutable {
		Il2CppClass* pCls{nullptr};
		{
			if (request->has_address())
			{
				pCls = il2cpp_klass_from_ptr(reinterpret_cast<void*>(request->address()));
			}
			else
			{
				const ::il2cppservice::ClassId* klass{&request->klass()};
				std::stack<const ::il2cppservice::ClassId*> classHeirarchy{};
				classHeirarchy.push(klass);
				while (klass->has_declaringtype())
				{
					klass = &klass->declaringtype();
					classHeirarchy.push(klass);
				}
				// reverse
				while (!classHeirarchy.empty())
				{
					const ::il2cppservice::ClassId* pParent{classHeirarchy.top()};
					if (pCls == nullptr)
					{
						const Il2CppClassInfo* pDeclaringClassInfo{
							pParent->isnullable() ? Il2CppContext::instance().FindNullableClass(pParent->namespaze(), pParent->name())
												  : Il2CppContext::instance().FindClass(pParent->namespaze(), pParent->name())};
						if (!pDeclaringClassInfo)
							return ::grpc::Status{grpc::StatusCode::NOT_FOUND, "Could not find class"};
						pCls = pDeclaringClassInfo->klass();
					}
					else
					{
						void* iter{nullptr};
						bool found{false};
						while (Il2CppClass* pNestedClass = il2cpp_class_get_nested_types(pCls, &iter))
						{
							if (pParent->name() == il2cpp_class_get_name(pNestedClass))
							{
								pCls = pNestedClass;
								found = true;
								break;
							}
						}
						if (!found)
							return ::grpc::Status{grpc::StatusCode::NOT_FOUND, "Could not find class"};
					}

					if (!pCls)
						return ::grpc::Status{grpc::StatusCode::NOT_FOUND, "Could not find class"};
					classHeirarchy.pop();
				}
			}
		}

		if (!pCls)
			return ::grpc::Status{grpc::StatusCode::NOT_FOUND, "Could not find class"};

		Il2CppClassInfo classInfo{pCls};

		response->mutable_typeinfo()->set_staticfieldsaddress(reinterpret_cast<uint64_t>(il2cpp_class_get_static_field_data(pCls)));
		SetClassId(response->mutable_typeinfo()->mutable_klassid(), pCls);

		if (pCls->fields)
		{
			for (int n{0}, m{pCls->field_count}; n < m; ++n)
			{
				::il2cppservice::Il2CppField* pFld{response->mutable_typeinfo()->mutable_fields()->Add()};
				FieldInfo* pFieldInfo{const_cast<FieldInfo*>(&pCls->fields[n])};
				const Il2CppType* pFieldType{il2cpp_field_get_type(pFieldInfo)};
				Il2CppClass* pFieldClassType{il2cpp_class_from_il2cpp_type(pFieldType)};

				const bool isStatic{(il2cpp_type_get_attrs(pFieldType) & FIELD_ATTRIBUTE_STATIC) == FIELD_ATTRIBUTE_STATIC};
				size_t offset{il2cpp_field_get_offset(pFieldInfo)};
				if (!!il2cpp_class_is_valuetype(pCls) && !isStatic)
					offset -= sizeof(Il2CppObject); // valueType field metadata incorrectly considers object header in member field offsets

				pFld->set_name(il2cpp_field_get_name(pFieldInfo));
				pFld->set_offset(static_cast<uint32_t>(offset));
				pFld->set_klassaddr(reinterpret_cast<uint64_t>(pFieldClassType));
				pFld->set_static_(isStatic);
			}
		}
		return ::grpc::Status::OK;
	})};
	return result.value_or(::grpc::Status::CANCELLED);
}

::grpc::Status Il2CppServiceImpl::PinObject(
	::grpc::ServerContext* context,
	const ::il2cppservice::PinObjectMessage* request,
	::il2cppservice::PinObjectMessage* response) noexcept
{
	std::optional<::grpc::Status> result{m_executionQueue.Invoke<::grpc::Status>([&]() mutable {
		Il2CppObject* pObj{il2cpp_object_from_ptr(reinterpret_cast<void*>(request->obj().address()))};

		if (!pObj)
			return ::grpc::Status{::grpc::StatusCode::FAILED_PRECONDITION, "Object not found"};

		uint32_t handle{il2cpp_gchandle_new(pObj, true)};

		response->CopyFrom(*request);
		response->mutable_obj()->set_handle(handle);
		return ::grpc::Status::OK;
	})};
	return result.value_or(::grpc::Status::CANCELLED);
}

::grpc::Status Il2CppServiceImpl::FreeObject(
	::grpc::ServerContext* context,
	const ::il2cppservice::FreeObjectRequest* request,
	::il2cppservice::FreeObjectResponse* response) noexcept
{
	std::optional<::grpc::Status> result{m_executionQueue.Invoke<::grpc::Status>([&]() mutable {
		il2cpp_gchandle_free(request->handle());
		return ::grpc::Status::OK;
	})};
	return result.value_or(::grpc::Status::CANCELLED);
}
