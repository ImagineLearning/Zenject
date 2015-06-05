using System;
using ModestTree;
using ModestTree.Util;
using Tuple = ModestTree.Util.Tuple;

namespace Zenject
{
    public static class KernelUtil
    {
        public static void BindTickable<TTickable>(DiContainer container, int priority) where TTickable : ITickable
        {
            container.Bind<ITickable>().ToSingle<TTickable>();
            BindTickablePriority<TTickable>(container, priority);
        }

        public static void BindTickablePriority<TTickable>(DiContainer container, int priority)
        {
            container.Bind<ModestTree.Util.Tuple<Type, int>>().ToInstance(Tuple.New(typeof(TTickable), priority));
        }
    }
}
