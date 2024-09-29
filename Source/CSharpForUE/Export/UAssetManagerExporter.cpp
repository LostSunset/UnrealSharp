﻿#include "UAssetManagerExporter.h"
#include "CSManager.h"
#include "Engine/AssetManager.h"

void UUAssetManagerExporter::ExportFunctions(FRegisterExportedFunction RegisterExportedFunction)
{
	EXPORT_FUNCTION(GetAssetManager)
}

void* UUAssetManagerExporter::GetAssetManager()
{
	UAssetManager& AssetManager = UAssetManager::Get();
	return FCSManager::Get().FindManagedObject(&AssetManager).GetIntPtr();
}
