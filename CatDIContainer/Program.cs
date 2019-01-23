using System;

namespace CatDIContainer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var root = new Cat()
            .Register<IFoo, Foo>(LifeTime.Transient)
            .Register<IBar>(_ => new Bar(), LifeTime.Scoped)
            .Register<IBaz, Baz>(LifeTime.Singleton);
            var cat1 = root.CreateChild();
            var cat2 = root.CreateChild();

            void GetServices<TService>(Cat cat)
            {
                cat.GetService<TService>();
                cat.GetService<TService>();
            }

            GetServices<IFoo>(cat1);
            GetServices<IBar>(cat1);
            GetServices<IBaz>(cat1);
            Console.WriteLine();
            GetServices<IFoo>(cat2);
            GetServices<IBar>(cat2);
            GetServices<IBaz>(cat2);
            Console.ReadKey();
        }
    }
}
