using System;

namespace Cheezburger.SchemaManager.Structure
{
    public static class CallbackHelper
    {
        public static T RunCallback<T>(Type delegateType, string callback, object[] args, Type[] argTypes)
        {
            object result = RunCallback(delegateType, callback, args, argTypes);
            if (result == null)
                return default(T);
            else
                return (T) result;
        }

        public static object RunCallback(Type delegateType, string callback, object[] args, Type[] argTypes)
        {
            if (callback == null)
                return null;

            var info = callback.Split(new[] { ',' }, 2);
            var methodName = info[0].Trim();
            var typeName = info[1].Trim();

            var type = Type.GetType(typeName, true, true);
            var method = type.GetMethod(methodName, argTypes);

            object oThis = null;
            if (!method.IsStatic)
                oThis = Activator.CreateInstance(type, false);

            var action = Delegate.CreateDelegate(delegateType, oThis, method);
            return action.DynamicInvoke(args);
        }
    }
}