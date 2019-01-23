using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CatDIContainer
{
    static class CatExtensions
    {
        public static Cat Register(this Cat cat, Type from, Type to, LifeTime lifeTime)
        {
            Func<Cat, Type[], object> factory = (_, arguments) => Create(_, to, arguments);
            cat.Register(new ServiceRegistry(from, lifeTime, factory));
            return cat;
        }

        public static Cat Register<TFrom, TTo>(this Cat cat, LifeTime lifeTime) where TTo : TFrom => cat.Register(typeof(TFrom), typeof(TTo), lifeTime);

        public static Cat Register<TServiceType>(this Cat cat, TServiceType instance)
        {
            Func<Cat, Type[], object> factory = (_, arguments) => instance;
            cat.Register(new ServiceRegistry(typeof(TServiceType), LifeTime.Singleton, factory));
            return cat;
        }

        public static Cat Register<TServiceType>(this Cat cat, Func<Cat, TServiceType> factory, LifeTime lifeTime)
        {
            cat.Register(new ServiceRegistry(typeof(TServiceType), lifeTime, (_, arguments) => factory(_)));
            return cat;
        }

        public static T GetService<T>(this Cat cat) => (T)cat.GetService(typeof(T));

        public static IEnumerable<T> GetServices<T>(this Cat cat) => cat.GetService<IEnumerable<T>>();

        public static bool HasRegistry<T>(this Cat cat) => cat.HasRegistry(typeof(T));
        public static bool HasRegistry(this Cat cat, Type serviceType) => cat._root._registries.ContainsKey(serviceType);

        private static object Create(Cat cat, Type type, Type[] GenericArguments)
        {
            if (GenericArguments.Length > 0)
            {
                type = type.MakeGenericType(GenericArguments);
            }

            //公共构造函数
            var constructors = type.GetConstructors();
            if (constructors.Length == 0)
            {
                throw new InvalidOperationException($"can not create the instance of {type} which does not have an public constructor.");
            }
            var construtor = constructors.FirstOrDefault(t => t.GetCustomAttributes(false).OfType<InjectionAttribute>().Any()) ?? constructors.First();
            var parameters = construtor.GetParameters();
            if (parameters.Length == 0)
            {
                //无参构造
                return Activator.CreateInstance(type);
            }

            //有参构造
            var arguments = new object[parameters.Length];
            for (int index = 0; index < arguments.Length; index++)
            {
                var parameter = parameters[index];
                var parameterType = parameter.ParameterType;
                if (cat.HasRegistry(parameterType))
                {
                    arguments[index] = cat.GetService(parameterType);
                }
                else if (parameter.HasDefaultValue)
                {
                    arguments[index] = parameter.DefaultValue;
                }
                else
                {
                    throw new InvalidOperationException($"can not create the instance of {type} whose constructor has non-registered parameter type(s)");
                }
            }
            return Activator.CreateInstance(type, arguments);
        }

    }
}
