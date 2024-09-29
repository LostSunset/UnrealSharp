﻿using EpicGames.Core;
using EpicGames.UHT.Types;
using UnrealSharpScriptGenerator.Utilities;

namespace UnrealSharpScriptGenerator.PropertyTranslators;

public class ContainerPropertyTranslator : PropertyTranslator
{
    public ContainerPropertyTranslator(string copyMarshallerName, string readOnlyMarshallerName, string marshallerName, string readOnlyInterfaceName, string interfaceName) : base(ContainerSupportedUsages)
    {
        CopyMarshallerName = copyMarshallerName;
        ReadOnlyMarshallerName = readOnlyMarshallerName;
        MarshallerName = marshallerName;
        ReadOnlyInterfaceName = readOnlyInterfaceName;
        InterfaceName = interfaceName;
    }

    public readonly string CopyMarshallerName;
    public readonly string ReadOnlyMarshallerName;
    public readonly string MarshallerName;
    
    public readonly string ReadOnlyInterfaceName;
    public readonly string InterfaceName;

    public override bool IsBlittable => false;
    public override bool NeedSetter => false;

    public override bool CanExport(UhtProperty property)
    {
        UhtContainerBaseProperty containerProperty = (UhtContainerBaseProperty) property;
        PropertyTranslator? translator = PropertyTranslatorManager.GetTranslator(containerProperty.ValueProperty);
        return translator != null && translator.CanExport(containerProperty.ValueProperty) && translator.IsSupportedAsInner();
    }

    public override string GetManagedType(UhtProperty property)
    {
        return GetWrapperInterface(property);
    }

    public override string GetMarshaller(UhtProperty property)
    {
        throw new System.NotImplementedException();
    }

    public override string ExportMarshallerDelegates(UhtProperty property)
    {
        throw new System.NotImplementedException();
    }

    public override void ExportPropertyGetter(GeneratorStringBuilder builder, UhtProperty property, string propertyManagedName)
    {
        UhtContainerBaseProperty containerProperty = (UhtContainerBaseProperty) property;
        PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(containerProperty.ValueProperty)!;
        
        string wrapperType = GetWrapperType(property);
        string marshallingDelegates = translator.ExportMarshallerDelegates(containerProperty.ValueProperty);

        builder.AppendLine($"{propertyManagedName}_Marshaller ??= new {wrapperType}(1, {propertyManagedName}_NativeProperty, {marshallingDelegates});");
        builder.AppendLine($"return {propertyManagedName}_Marshaller.FromNative(IntPtr.Add(NativeObject, {propertyManagedName}_Offset), 0);");
    }

    public override void ExportPropertyVariables(GeneratorStringBuilder builder, UhtProperty property, string propertyEngineName)
    {
        base.ExportPropertyVariables(builder, property, propertyEngineName);
        builder.AppendLine($"static IntPtr {propertyEngineName}_NativeProperty;");

        string wrapperType = GetWrapperType(property);
        if (property.IsOuter<UhtScriptStruct>())
        {
            builder.AppendLine($"static {wrapperType} {propertyEngineName}_Marshaller = null;");
        }
        else
        {
            builder.AppendLine($"{wrapperType} {propertyEngineName}_Marshaller = null;");
        }
    }

    public override void ExportParameterVariables(GeneratorStringBuilder builder, UhtFunction function,
        string nativeMethodName,
        UhtProperty property, string propertyEngineName)
    {
        base.ExportParameterVariables(builder, function, nativeMethodName, property, propertyEngineName);
        builder.AppendLine($"static IntPtr {nativeMethodName}_{propertyEngineName}_NativeProperty;");
        
        string wrapperType = GetWrapperType(property);
        if (function.FunctionFlags.HasAnyFlags(EFunctionFlags.Static))
        {
            builder.AppendLine($"static {wrapperType} {nativeMethodName}_{propertyEngineName}_Marshaller = null;");
        }
        else
        {
            builder.AppendLine($"{wrapperType} {nativeMethodName}_{propertyEngineName}_Marshaller = null;");
        }
    }

    public override void ExportParameterStaticConstructor(GeneratorStringBuilder builder, UhtProperty property,
        UhtFunction function, string propertyEngineName, string functionName)
    {
        base.ExportParameterStaticConstructor(builder, property, function, propertyEngineName, functionName);
        builder.AppendLine($"{functionName}_{propertyEngineName}_NativeProperty = {ExporterCallbacks.FPropertyCallbacks}.CallGetNativePropertyFromName({functionName}_NativeFunction, \"{propertyEngineName}\");");
    }

    public override void ExportPropertyStaticConstructor(GeneratorStringBuilder builder, UhtProperty property, string nativePropertyName)
    {
        base.ExportPropertyStaticConstructor(builder, property, nativePropertyName);
        builder.AppendLine($"{nativePropertyName}_NativeProperty = {ExporterCallbacks.FPropertyCallbacks}.CallGetNativePropertyFromName(NativeClassPtr, \"{property.EngineName}\");");
    }

    public override string GetNullValue(UhtProperty property)
    {
        return "null";
    }
    
    public override string ConvertCPPDefaultValue(string defaultValue, UhtFunction function, UhtProperty parameter)
    {
        throw new System.NotImplementedException();
    }
    
    public override void ExportFromNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName, string assignmentOrReturn,
        string sourceBuffer, string offset, bool bCleanupSourceBuffer, bool reuseRefMarshallers)
    {
        UhtContainerBaseProperty containerProperty = (UhtContainerBaseProperty) property;
        
        UhtProperty valueProperty = containerProperty.ValueProperty;
        PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(valueProperty)!;
        
        string nativeProperty = $"{propertyName}_NativeProperty";
        string marshaller = $"{propertyName}_Marshaller";

        if (property.Outer is UhtFunction function)
        {
            string nativeMethodName = function.SourceName;
            nativeProperty = $"{nativeMethodName}_{nativeProperty}";
            marshaller = $"{nativeMethodName}_{marshaller}";
        }
        
        string innerType = translator.GetManagedType(valueProperty);
        string marshallerType = $"{CopyMarshallerName}<{innerType}>";
        string marshallingDelegates = translator.ExportMarshallerDelegates(valueProperty);

        if (!reuseRefMarshallers)
        {
            builder.AppendLine($"{marshaller} ??= new {marshallerType}({nativeProperty}, {marshallingDelegates});");
            
            builder.AppendLine($"IntPtr {propertyName}_NativeBuffer = IntPtr.Add({sourceBuffer}, {offset});");
        }
        
        builder.AppendLine($"{assignmentOrReturn} {marshaller}.FromNative({propertyName}_NativeBuffer, 0);");
        
        if (bCleanupSourceBuffer)
        {
            ExportCleanupMarshallingBuffer(builder, property, propertyName);
        }
    }
    
    public override void ExportToNative(GeneratorStringBuilder builder, UhtProperty property, string propertyName, string destinationBuffer,
        string offset, string source)
    {
        UhtContainerBaseProperty containerProperty = (UhtContainerBaseProperty) property;
        UhtProperty valueProperty = containerProperty.ValueProperty;
        PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(valueProperty)!;
        
        string nativeProperty = $"{propertyName}_NativeProperty";
        string marshaller = $"{propertyName}_Marshaller";

        if (property.Outer is UhtFunction function)
        {
            string nativeMethodName = function.SourceName;
            nativeProperty = $"{nativeMethodName}_{nativeProperty}";
            marshaller = $"{nativeMethodName}_{marshaller}";
        }
        
        string innerType = translator.GetManagedType(valueProperty);
        string marshallerType = $"{CopyMarshallerName}<{innerType}>";
        
        string marshallingDelegates = translator.ExportMarshallerDelegates(valueProperty);
        builder.AppendLine($"{marshaller} ??= new {marshallerType}({nativeProperty}, {marshallingDelegates});");
        builder.AppendLine($"IntPtr {propertyName}_NativeBuffer = IntPtr.Add({destinationBuffer}, {offset});");
        builder.AppendLine($"{marshaller}.ToNative({propertyName}_NativeBuffer, 0, {source});");
    }

    public override void ExportCleanupMarshallingBuffer(GeneratorStringBuilder builder, UhtProperty property, string paramName)
    {
        UhtFunction function = (UhtFunction) property.Outer!;
        string marshaller = $"{function.SourceName}_{paramName}_Marshaller";
        builder.AppendLine($"{marshaller}.DestructInstance({paramName}_NativeBuffer, 0);");
    }
    
    private string GetWrapperType(UhtProperty property)
    {
        bool isStructProperty = property.IsOuter<UhtScriptStruct>();
        bool isParameter = property.IsOuter<UhtFunction>();
        UhtContainerBaseProperty containerProperty = (UhtContainerBaseProperty) property;
        PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(containerProperty.ValueProperty)!;
        
        string containerType = isStructProperty || isParameter ? CopyMarshallerName : property.PropertyFlags.HasAnyFlags(EPropertyFlags.BlueprintReadOnly) ? ReadOnlyMarshallerName : MarshallerName;

        return $"{containerType}<{translator.GetManagedType(containerProperty.ValueProperty)}>";
    }

    private string GetWrapperInterface(UhtProperty property)
    {
        UhtContainerBaseProperty containerProperty = (UhtContainerBaseProperty) property;
        PropertyTranslator translator = PropertyTranslatorManager.GetTranslator(containerProperty.ValueProperty)!;
        string innerManagedType = translator.GetManagedType(containerProperty.ValueProperty);
        string interfaceType = property.PropertyFlags.HasAnyFlags(EPropertyFlags.BlueprintReadOnly) ? ReadOnlyInterfaceName : InterfaceName;
        return $"System.Collections.Generic.{interfaceType}<{innerManagedType}>";
    }
}