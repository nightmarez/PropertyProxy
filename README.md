# PropertyProxy
A simple way to create proxy objects

✍✍✍ **Feedback is welcome** ✅✅✅

💚💚💚 **Stars are welcome** 💚💚💚

## Usage
Add `PropertyProxyFactory.cs` file to your project

Add `[PropertyProxy]` attribute to properties which you want to proxify
```csharp
class MyClass
{
    public int X { get; set; }
    
    [PropertyProxy]
    public int Y { get; set; }
}
```

Create proxy factory
```csharp
var factory = new PropertyProxyFactory();
```

Now you can create proxies for your MyClass's objects
```csharp
var myObject = new MyClass();
dynamic proxy = factory.CreateProxy(myObject);

// Set properties
proxy.Y = 10; // Has access! myObject.Y will set to 10
proxy.X = 20; // Error!

// Get properties
Console.WriteLine($@"Y: {proxy.Y}"); // Works good
Console.WriteLine($@"X: {proxy.X}, Y: {proxy.Y}"); // Error! You have not access to property X

// Access trough reflection
var proxy2 = factory.CreateProxy(myObject);
Type proxyType = proxy2.GetType();
foreach (PropertyInfo property in proxyType.GetProperties())
{
    string propertyName = property.Name;
    property.SetValue(proxy2, 20);
    string propertyValue = property.GetValue(proxy2).ToString();
    Console.WriteLine($@"{propertyName} = {propertyValue}"); // You will see only "Y = 20"
}
```

Using through interface:
```csharp
public IMyInterface
{
    int Y { get; set; }
}

// ...

IMyInterface proxy3 = factory.CreateProxy<IMyInterface>(myObject);
proxy3.Y = 30; // Has access!

// You can't get access to property X anyway
Console.WriteLine($@"X = {proxy3.GetType().GetProperty("X")!.GetValue(proxy3)}"); // Error
```

You can optimize proxy types generation by calling previously method `GenerateFor` with types or object what proxy you want generate for
```csharp
// For types:
factory.GenerateFor(MyClass1, MyClass2, typeof(objectOfMyClass3));

// For objects:
factory.GenerateFor(objectOfMyClass1, objectOfMyClass2, objectOfMyClass3);
```
When you calling `GenerateFor` method, there is will be created one dynamic assembly for all enumerated types or objects

## License

The sources is available as open source under the terms of the [MIT License](http://opensource.org/licenses/MIT).
