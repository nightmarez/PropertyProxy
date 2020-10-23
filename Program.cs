using System;
using System.Reflection;

namespace PropertyProxy
{
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

    public class Program
    {
        static void Main()
        {
            var factory = new PropertyProxyFactory();

            var myObject = new MyClass();
            dynamic proxy = factory.CreateProxy(myObject);

            factory.GenerateFor(typeof(MyClass), typeof(MyClass1));
            factory.GenerateFor(myObject);

            // Set properties
            proxy.Y = 10; // Has access! myObject.Y will set to 10
            // proxy.X = 20; // Error!

            // Get properties
            Console.WriteLine($@"Y = {proxy.Y}"); // Works good
            // Console.WriteLine($@"X = {proxy.X}, Y = {proxy.Y}"); // Error! You have not access to property X

            // Access trough reflection
            var proxy2 = factory.CreateProxy(myObject);
            Type proxyType = proxy2.GetType();
            foreach (PropertyInfo property in proxyType.GetProperties())
            {
                string propertyName = property.Name;
                property.SetValue(proxy2, 20);
                string propertyValue = property.GetValue(proxy2).ToString();
                Console.WriteLine($@"{propertyName} = {propertyValue}");
            }

            // Interface usage
            var myObject3 = new MyClass3();
            IMyClass3 proxy3 = factory.CreateProxy<IMyClass3>(myObject3);
            myObject3.X = 30;
            Console.WriteLine($@"X = {proxy3.X}");

            Console.ReadKey();
        }
    }
}
