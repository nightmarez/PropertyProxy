using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

public class PropertyProxyFactory
{
    public sealed class PropertyProxyAttribute: Attribute { }

    private int _counter;
    private readonly Dictionary<string, Type> _types = new Dictionary<string, Type>();

    private string GenAssemblyName()
    {
        return $@"ProxyAssembly-{Guid.NewGuid()}-{++_counter}.dll";
    }

    private string GenProxyTypeName(Type sourceType)
    {
        return $@"{sourceType.Name}-Proxy-{Guid.NewGuid()}-{++_counter}";
    }

    public void GenerateFor(params Type[] types)
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

        foreach (var sourceType in types)
        {
            string sourceTypeName = sourceType.AssemblyQualifiedName ?? string.Empty;

            if (_types.ContainsKey(sourceTypeName))
            {
                continue;
            }

            TypeBuilder typeBuilder = moduleBuilder.DefineType(GenProxyTypeName(sourceType), TypeAttributes.Class | TypeAttributes.Public, typeof(object));
            FieldBuilder ownerField = typeBuilder.DefineField("_owner", sourceType, FieldAttributes.Private);

            foreach (PropertyInfo prop in sourceType.GetProperties())
                foreach (object attribute in prop.GetCustomAttributes(true))
                    if (attribute is PropertyProxyAttribute)
                    {
                        PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(prop.Name,
                            PropertyAttributes.HasDefault,
                            prop.PropertyType,
                            null);

                        MethodBuilder methodGet = typeBuilder.DefineMethod("Get_" + prop.Name,
                            MethodAttributes.Public,
                            prop.PropertyType,
                            new Type[] { });

                        ILGenerator ilGet = methodGet.GetILGenerator();

                        ilGet.Emit(OpCodes.Ldarg_0);
                        ilGet.Emit(OpCodes.Ldfld, ownerField);
                        ilGet.EmitCall(OpCodes.Callvirt, prop.GetGetMethod(), null);
                        ilGet.Emit(OpCodes.Ret);

                        MethodBuilder methodSet = typeBuilder.DefineMethod("Set_" + prop.Name,
                            MethodAttributes.Public,
                            null,
                            new[] { prop.PropertyType });

                        ILGenerator ilSet = methodSet.GetILGenerator();

                        ilSet.Emit(OpCodes.Ldarg_0);
                        ilSet.Emit(OpCodes.Ldfld, ownerField);
                        ilSet.Emit(OpCodes.Ldarg_1);
                        ilSet.EmitCall(OpCodes.Callvirt, prop.GetSetMethod(), new[] { prop.PropertyType });
                        ilSet.Emit(OpCodes.Ret);

                        propertyBuilder.SetGetMethod(methodGet);
                        propertyBuilder.SetSetMethod(methodSet);

                        break;
                    }

            ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(object) });
            ILGenerator ilGen = constructorBuilder.GetILGenerator();

            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldarg_1);
            ilGen.Emit(OpCodes.Stfld, ownerField);

            ilGen.Emit(OpCodes.Ret);

            Type type = typeBuilder.CreateType();
            _types.Add(sourceTypeName, type);
        }
    }

    public void GenerateFor(params object[] objects)
    {
        GenerateFor(objects.Select(obj => obj.GetType()).ToArray());
    }

    public object CreateProxy(object source)
    {
        Type sourceType = source.GetType();
        GenerateFor(sourceType);
        string sourceTypeName = sourceType.AssemblyQualifiedName ?? string.Empty;
        return Activator.CreateInstance(_types[sourceTypeName], source);
    }
}
