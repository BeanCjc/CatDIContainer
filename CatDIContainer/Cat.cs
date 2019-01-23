using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CatDIContainer
{
    class Cat : IServiceProvider, IDisposable
    {
        internal Cat _root;
        internal ConcurrentDictionary<Type, ServiceRegistry> _registries;
        private ConcurrentDictionary<ServiceRegistry, object> _services;
        private ConcurrentBag<IDisposable> _disposables;
        private volatile bool _disposed;
        public Cat()
        {
            _root = this;
            _registries = new ConcurrentDictionary<Type, ServiceRegistry>();
            _services = new ConcurrentDictionary<ServiceRegistry, object>();
            _disposables = new ConcurrentBag<IDisposable>();
        }
        internal Cat(Cat parent)
        {
            _root = parent._root;
            _registries = parent._registries;
            _services = new ConcurrentDictionary<ServiceRegistry, object>();
            _disposables = new ConcurrentBag<IDisposable>();
        }

        public Cat CreateChild() => new Cat(this);
        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("Cat");
            }
        }
        public Cat Register(ServiceRegistry serviceRegistry)
        {
            EnsureNotDisposed();
            if (_registries.TryGetValue(serviceRegistry.ServiceType, out var existing))
            {
                _registries[serviceRegistry.ServiceType] = serviceRegistry;
                serviceRegistry.Next = existing;
            }
            else
            {
                _registries[serviceRegistry.ServiceType] = serviceRegistry;
            }
            return this;
        }

        private object GetServiceCore(ServiceRegistry registry, Type[] genericArguments)
        {
            var serviecType = registry.ServiceType;
            object CreateOrGet(ConcurrentDictionary<ServiceRegistry, object> services, ConcurrentBag<IDisposable> disposables)
            {
                if (services.TryGetValue(registry, out var service))
                {
                    return service;
                }
                service = registry.Factory(this, genericArguments);
                services[registry] = service;
                var disposable = service as IDisposable;
                if (disposable != null)
                {
                    disposables.Add(disposable);
                }
                return service;
            }

            switch (registry.LifeTime)
            {
                case LifeTime.Singleton:
                    return CreateOrGet(_root._services, _root._disposables);
                case LifeTime.Scoped:
                    return CreateOrGet(_services, _disposables);
                default:
                    {
                        var service = registry.Factory(this, genericArguments);
                        var disposable = service as IDisposable;
                        if (disposable != null)
                        {
                            _disposables.Add(disposable);
                        }
                        return service;
                    }
            }
        }

        public object GetService(Type serviceType)
        {
            EnsureNotDisposed();
            //返回自身
            if (serviceType == typeof(Cat) || serviceType == typeof(IServiceProvider))
            {
                return this;
            }

            //返回集合
            ServiceRegistry serviceRegistry;
            if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var elementType = serviceType.GetGenericArguments()[0];
                if (!_registries.TryGetValue(elementType, out serviceRegistry))
                {
                    return Array.CreateInstance(elementType, 0);
                }
                var registries = serviceRegistry.AsEnumerable();
                var services = registries.Select(t => GetServiceCore(t, new Type[0])).ToArray();
                Array array = Array.CreateInstance(elementType, services.Length);
                services.CopyTo(array, 0);
                return array;
            }

            //返回泛型类
            if (serviceType.IsGenericType && !_registries.ContainsKey(serviceType))
            {
                var definition = serviceType.GetGenericTypeDefinition();
                return _registries.TryGetValue(definition, out serviceRegistry) ? GetServiceCore(serviceRegistry, serviceType.GetGenericArguments()) : null;
            }

            //返回指定类
            return _registries.TryGetValue(serviceType, out serviceRegistry) ? GetServiceCore(serviceRegistry, new Type[0]) : null;
        }

        public void Dispose()
        {
            _disposed = true;
            foreach (var disposed in _disposables)
            {
                disposed.Dispose();
            }
            while (!_disposables.IsEmpty)
            {
                _disposables.TryTake(out _);
            }
            _services.Clear();
        }
    }
}
