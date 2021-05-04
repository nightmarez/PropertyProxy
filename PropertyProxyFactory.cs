using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

public class PropertyProxyFactory
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class PropertyProxyAttribute : Attribute { }

    private readonly Dictionary<string, Type> _types = new Dictionary<string, Type>();

    private string GenAssemblyName()
    {
        return $"ProxyAssembly-{Guid.NewGuid()}.dll";
    }

    private string GenProxyTypeName(Type sourceType)
    {
        return $"{sourceType.Name}-Proxy-{Guid.NewGuid()}";
    }

    private void InnerGenerateFor(IReadOnlyList<Type> types, IReadOnlyList<Type> interfaces = null)
    {
        if (types
            .Select(t => t.AssemblyQualifiedName ?? string.Empty)
            .All(name => _types.ContainsKey(name)))
        {
            return;
        }

        var assemblyName = new AssemblyName(GenAssemblyName());
        AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder moduleBuilder = assembly.DefineDynamicModule(assemblyName.Name);

        for (int i = 0; i < types.Count; ++i)
        {
            var sourceType = types[i];
            string sourceTypeName = sourceType.AssemblyQualifiedName ?? string.Empty;

            if (_types.ContainsKey(sourceTypeName))
            {
                continue;
            }

            TypeBuilder typeBuilder = moduleBuilder.DefineType(
                GenProxyTypeName(sourceType),
                TypeAttributes.Class | TypeAttributes.Public,
                typeof(object));
            Type interfaceType = null;

            if (interfaces is { } && i < interfaces.Count)
            {
                interfaceType = interfaces[i];
                typeBuilder.AddInterfaceImplementation(interfaceType);
            }

            FieldBuilder ownerField = typeBuilder.DefineField("_owner", sourceType, FieldAttributes.Private);
            var proxyProperties = new List<PropertyInfo>();

            if (interfaceType is { })
            {
                PropertyInfo[] interfaceProperties = interfaceType.GetProperties();

                proxyProperties.AddRange(
                    sourceType
                        .GetProperties()
                        .Where(prop => interfaceProperties.Any(interfaceProperty => interfaceProperty.Name == prop.Name)));
            }
            else
            {
                proxyProperties.AddRange(
                    sourceType
                        .GetProperties()
                        .Where(prop => prop.GetCustomAttributes(true).Any(attribute => attribute is PropertyProxyAttribute)));
            }

            foreach (PropertyInfo prop in proxyProperties)
            {
                PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(
                    prop.Name,
                    PropertyAttributes.HasDefault,
                    prop.PropertyType,
                    null);

                MethodBuilder methodGet = typeBuilder.DefineMethod(
                    $"get_{prop.Name}",
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    prop.PropertyType,
                    new Type[] { });

                methodGet.SetImplementationFlags(MethodImplAttributes.IL);
                ILGenerator ilGet = methodGet.GetILGenerator();

                ilGet.Emit(OpCodes.Ldarg_0);
                ilGet.Emit(OpCodes.Ldfld, ownerField);
                ilGet.EmitCall(OpCodes.Callvirt, prop.GetGetMethod(), null);
                ilGet.Emit(OpCodes.Ret);

                MethodBuilder methodSet = typeBuilder.DefineMethod(
                    $"set_{prop.Name}",
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    null,
                    new[] { prop.PropertyType });

                methodSet.SetImplementationFlags(MethodImplAttributes.IL);
                ILGenerator ilSet = methodSet.GetILGenerator();

                ilSet.Emit(OpCodes.Ldarg_0);
                ilSet.Emit(OpCodes.Ldfld, ownerField);
                ilSet.Emit(OpCodes.Ldarg_1);
                ilSet.EmitCall(OpCodes.Callvirt, prop.GetSetMethod(), new[] { prop.PropertyType });
                ilSet.Emit(OpCodes.Ret);

                propertyBuilder.SetGetMethod(methodGet);
                propertyBuilder.SetSetMethod(methodSet);

                if (interfaceType is { })
                {
                    var originalGetter = interfaceType.GetMethod($"get_{prop.Name}");
                    var originalSetter = interfaceType.GetMethod($"set_{prop.Name}");
                    typeBuilder.DefineMethodOverride(methodGet, originalGetter!);
                    typeBuilder.DefineMethodOverride(methodSet, originalSetter!);
                }
            }

            ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                new[] { typeof(object) });
            ILGenerator ilGen = constructorBuilder.GetILGenerator();

            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldarg_1);
            ilGen.Emit(OpCodes.Stfld, ownerField);

            ilGen.Emit(OpCodes.Ret);

            Type type = typeBuilder.CreateType();
            _types.Add(sourceTypeName, type);
        }
    }

    public void GenerateFor(params Type[] types)
    {
        InnerGenerateFor(types);
    }

    public void GenerateFor(params object[] objects)
    {
        InnerGenerateFor(objects.Select(obj => obj.GetType()).ToArray());
    }

    public object CreateProxy(object source)
    {
        Type sourceType = source.GetType();
        GenerateFor(sourceType);
        string sourceTypeName = sourceType.AssemblyQualifiedName ?? string.Empty;
        return Activator.CreateInstance(_types[sourceTypeName], source);
    }

    public T CreateProxy<T>(object source)
    {
        Type sourceType = source.GetType();
        InnerGenerateFor(new[] { sourceType }, new[] { typeof(T) });
        string sourceTypeName = sourceType.AssemblyQualifiedName ?? string.Empty;
        return (T)Activator.CreateInstance(_types[sourceTypeName], source);
    }
}
