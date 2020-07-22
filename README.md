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
```

## License

The sources is available as open source under the terms of the [MIT License](http://opensource.org/licenses/MIT).
