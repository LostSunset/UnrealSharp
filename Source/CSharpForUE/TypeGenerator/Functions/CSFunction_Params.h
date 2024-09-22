﻿#pragma once

#include "CoreMinimal.h"
#include "CSFunction.h"
#include "CSFunction_Params.generated.h"

UCLASS()
class CSHARPFORUE_API UCSFunction_Params : public UCSFunctionBase
{
	GENERATED_BODY()

public:

	
	static void InvokeManagedMethod_Params(UObject* ObjectToInvokeOn, FFrame& Stack, RESULT_DECL);
	
};
