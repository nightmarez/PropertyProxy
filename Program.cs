using System.Reflection;

var myObject = new MyClass();
dynamic proxy = PropertyProxyFactory.Instance.CreateProxy(myObject);

PropertyProxyFactory.Instance.GenerateFor(typeof(MyClass), typeof(MyClass1));
PropertyProxyFactory.Instance.GenerateFor(myObject);

for (int i = 0; i < 30; i += 3)
{
    // Set properties
    proxy.Y = i; // Has access! myObject.Y will set to 10
    // proxy.X = 20; // Error!

    // Get properties
    Console.WriteLine($@"Y = {proxy.Y}"); // Works good
    // Console.WriteLine($@"X = {proxy.X}, Y = {proxy.Y}"); // Error! You have not access to property X

    // Access trough reflection
    var proxy2 = PropertyProxyFactory.Instance.CreateProxy(myObject);
    Type proxyType = proxy2.GetType();
    foreach (PropertyInfo property in proxyType.GetProperties())
    {
        string propertyName = property.Name;
        property.SetValue(proxy2, i + 1);
        string propertyValue = property.GetValue(proxy2).ToString();
        Console.WriteLine($@"{propertyName} = {propertyValue}");
    }

    // Interface usage
    var myObject3 = new MyClass3();
    IMyClass3 proxy3 = PropertyProxyFactory.Instance.CreateProxy<IMyClass3>(myObject3);
    myObject3.X = i + 2;
    Console.WriteLine($@"X = {proxy3.X}");
}

public class MyClass
{
    public int X { get; set; }

    [PropertyProxyFactory.PropertyProxy]
    public int Y { get; set; }
}

public class MyClass1
{
    public int X { get; set; }

    [PropertyProxyFactory.PropertyProxy]
    public int Y { get; set; }
}

public class MyClass3
{
    public int X { get; set; }

    public int Y { get; set; }
}

public interface IMyClass3
{
    int X { get; set; }
}
