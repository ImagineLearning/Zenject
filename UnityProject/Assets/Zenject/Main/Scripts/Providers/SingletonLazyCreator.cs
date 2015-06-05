using System;
using System.Collections.Generic;
using ModestTree;

namespace Zenject
{
    internal class SingletonLazyCreator
    {
        int _referenceCount;
        object _instance;
        SingletonProviderMap _owner;
        DiContainer _container;
        bool _hasInstance;
        Func<InjectContext, object> _createMethod;
        SingletonId _id;

        public SingletonLazyCreator(
            DiContainer container, SingletonProviderMap owner,
            SingletonId id, Func<InjectContext, object> createMethod = null)
        {
            _container = container;
            _owner = owner;
            _id = id;
            _createMethod = createMethod;
        }

        public bool HasCustomCreateMethod
        {
            get
            {
                return _createMethod != null;
            }
        }

        public void IncRefCount()
        {
            _referenceCount += 1;
        }

        public void DecRefCount()
        {
            _referenceCount -= 1;

            if (_referenceCount <= 0)
            {
                _owner.RemoveCreator(_id);
            }
        }

        public void SetInstance(object instance)
        {
            Assert.IsNull(_instance);
            Assert.That(instance != null || _container.AllowNullBindings);

            _instance = instance;
            // We need this flag for validation
            _hasInstance = true;
        }

        public bool HasInstance()
        {
            if (_hasInstance)
            {
                Assert.That(_container.AllowNullBindings || _instance != null);
            }

            return _hasInstance;
        }

        public Type GetInstanceType()
        {
            return _id.Type;
        }

        public object GetInstance(InjectContext context)
        {
            if (!_hasInstance)
            {
                _instance = Instantiate(context);

                if (_instance == null)
                {
                    throw new ZenjectResolveException(
                        "Unable to instantiate type '{0}' in SingletonLazyCreator".Fmt(context.MemberType));
                }

                _hasInstance = true;
            }

            return _instance;
        }

        object Instantiate(InjectContext context)
        {
            if (_createMethod != null)
            {
                return _createMethod(context);
            }

            var concreteType = GetTypeToInstantiate(context.MemberType);
            return _container.InstantiateExplicit(
                concreteType, new List<TypeValuePair>(), context);
        }

        Type GetTypeToInstantiate(Type contractType)
        {
            if (_id.Type.IsOpenGenericType())
            {
                Assert.That(!contractType.IsAbstract());
                Assert.That(contractType.GetGenericTypeDefinition() == _id.Type);
                return contractType;
            }

            Assert.That(_id.Type.DerivesFromOrEqual(contractType));
            return _id.Type;
        }
    }
}
