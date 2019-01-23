using System;
using System.Collections.Generic;
using System.Text;

namespace CatDIContainer
{
    [AttributeUsage(AttributeTargets.Constructor)]
    class InjectionAttribute:Attribute
    {
    }
}
