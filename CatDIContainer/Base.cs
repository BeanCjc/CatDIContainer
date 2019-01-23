using System;
using System.Collections.Generic;
using System.Text;

namespace CatDIContainer
{
    class Base : IDisposable
    {
        public Base()
        {
            Console.WriteLine($"An instance of {GetType().Name} is created!");
        }
        public void Dispose()
        {
            Console.WriteLine($"The instance of {GetType().Name} is disposed!");
        }
    }
}
