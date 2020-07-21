using System;
using System.Reflection;
using System.Reflection.Emit;

public class PropertyProxyFactory
{
    public sealed class PropertyProxyAttribute: Attribute { }

    private static int _counter;

    private static string GenAssemblyName()
    {
        return $@"ProxyAssembly-{Guid.NewGuid()}-{++_counter}.dll";
    }

    private static string GenProxyTypeName(Type sourceType)
    {
        return $@"{sourceType.Name}-Proxy-{Guid.NewGuid()}-{++_counter}";
    }

    public static object CreateProxy(object source)
    {
        var assemblyName = new AssemblyName(GenAssemblyName());
        AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder moduleBuilder = assembly.DefineDynamicModule(assemblyName.Name);
        Type sourceType = source.GetType();
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
        return Activator.CreateInstance(type, source);
    }
}
