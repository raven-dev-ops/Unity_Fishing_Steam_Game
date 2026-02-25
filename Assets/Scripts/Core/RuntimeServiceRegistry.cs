using System;
using System.Collections.Generic;
using UnityEngine;

namespace RavenDevOps.Fishing.Core
{
    public static class RuntimeServiceRegistry
    {
        private static readonly Dictionary<Type, UnityEngine.Object> ServicesByType = new Dictionary<Type, UnityEngine.Object>();
        private static readonly Dictionary<Type, object> InterfaceServicesByType = new Dictionary<Type, object>();

        public static void Register<T>(T service) where T : UnityEngine.Object
        {
            if (service == null)
            {
                return;
            }

            ServicesByType[typeof(T)] = service;
        }

        public static void Unregister<T>(T service) where T : UnityEngine.Object
        {
            if (service == null)
            {
                return;
            }

            var type = typeof(T);
            if (ServicesByType.TryGetValue(type, out var existing) && existing == service)
            {
                ServicesByType.Remove(type);
            }
        }

        public static bool TryGet<T>(out T service) where T : UnityEngine.Object
        {
            if (ServicesByType.TryGetValue(typeof(T), out var value) && value != null)
            {
                service = value as T;
                return service != null;
            }

            service = null;
            return false;
        }

        public static T Get<T>() where T : UnityEngine.Object
        {
            TryGet(out T service);
            return service;
        }

        public static bool Resolve<T>(ref T target, MonoBehaviour owner, bool warnIfMissing = true) where T : UnityEngine.Object
        {
            if (target != null)
            {
                return true;
            }

            if (TryGet(out target))
            {
                return true;
            }

            if (warnIfMissing && owner != null)
            {
                Debug.LogWarning($"{owner.GetType().Name}: Missing dependency {typeof(T).Name}. Assign in inspector or register during bootstrap.");
            }

            return false;
        }

        public static void Clear()
        {
            ServicesByType.Clear();
            InterfaceServicesByType.Clear();
        }

        public static void RegisterInterface<TInterface>(UnityEngine.Object service) where TInterface : class
        {
            if (service == null)
            {
                return;
            }

            if (!(service is TInterface typedService))
            {
                Debug.LogWarning($"RuntimeServiceRegistry: attempted to register {service.GetType().Name} as {typeof(TInterface).Name}, but it does not implement that interface.");
                return;
            }

            InterfaceServicesByType[typeof(TInterface)] = typedService;
        }

        public static void UnregisterInterface<TInterface>(UnityEngine.Object service) where TInterface : class
        {
            if (service == null)
            {
                return;
            }

            var type = typeof(TInterface);
            if (!InterfaceServicesByType.TryGetValue(type, out var existing))
            {
                return;
            }

            if (ReferenceEquals(existing, service))
            {
                InterfaceServicesByType.Remove(type);
            }
        }

        public static bool TryGetInterface<TInterface>(out TInterface service) where TInterface : class
        {
            if (InterfaceServicesByType.TryGetValue(typeof(TInterface), out var value) && value is TInterface typed)
            {
                service = typed;
                return true;
            }

            service = null;
            return false;
        }

        public static bool ResolveInterface<TInterface>(ref TInterface target, MonoBehaviour owner, bool warnIfMissing = true) where TInterface : class
        {
            if (target != null)
            {
                return true;
            }

            if (TryGetInterface(out target))
            {
                return true;
            }

            if (warnIfMissing && owner != null)
            {
                Debug.LogWarning($"{owner.GetType().Name}: Missing dependency {typeof(TInterface).Name}. Assign in inspector or register during bootstrap.");
            }

            return false;
        }
    }
}
