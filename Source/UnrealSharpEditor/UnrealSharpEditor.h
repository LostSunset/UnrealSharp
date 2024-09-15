﻿#pragma once

#include "CoreMinimal.h"
#include "Modules/ModuleManager.h"
#include "Containers/Ticker.h"

class FUnrealSharpEditorModule : public IModuleInterface
{
public:

    // IModuleInterface interface begin
    virtual void StartupModule() override;
    virtual void ShutdownModule() override;
    // End
    
    void OnCSharpCodeModified(const TArray<struct FFileChangeData>& ChangedFiles);
    static void StartHotReload();

    bool IsReloading() const { return bIsReloading; }
    
    static void OpenSolution();


private:
    
    static FString SelectArchiveDirectory();

    static void RunGame(FString ExecutablePath);

    void OnUnrealSharpInitialized();

    static void OnCreateNewProject();
    static void OnRegenerateSolution();

    static void PackageProject();

    TSharedRef<SWidget> GenerateUnrealSharpMenu();
    
    static void OpenNewProjectDialog(const FString& SuggestedProjectName = FString());

    static void SuggestProjectSetup();
    
    bool Tick(float DeltaTime);
    
    void RegisterCommands();
    void RegisterMenu();

    static FString QuotePath(const FString& Path);

    FTickerDelegate TickDelegate;
    FTSTicker::FDelegateHandle TickDelegateHandle;
    bool bIsReloading = false;
    TSharedPtr<FUICommandList> UnrealSharpCommands;
};
