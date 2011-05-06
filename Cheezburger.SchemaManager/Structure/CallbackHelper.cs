// Copyright (C) 2011 by Cheezburger, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

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