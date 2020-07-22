using System;

namespace PropertyProxy
{
    public class Test
    {
        [PropertyProxyFactory.PropertyProxy]
        public int X { get; set; }

        [PropertyProxyFactory.PropertyProxy]
        public int Y { get; set; }
    }

    public class Program
    {
        static void Main()
        {
            var factory = new PropertyProxyFactory();
            var test = new Test();

            dynamic proxy1 = factory.CreateProxy(test);
            proxy1.X = 10;

            dynamic proxy2 = factory.CreateProxy(test);
            proxy2.Y = 20;

            Console.WriteLine($@"X: {test.X}, Y: {test.Y}");
            Console.ReadKey();
        }
    }
}
