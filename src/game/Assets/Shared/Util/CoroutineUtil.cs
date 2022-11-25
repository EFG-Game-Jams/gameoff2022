using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Util
{
    public static class CoroutineUtil
    {
        /// <summary>
        /// Calls onComplete with thrown exception, or null if no exception was thrown.
        /// </summary>
        public static IEnumerator WrapThrowingEnumerator(IEnumerator enumerator, Action<Exception> onComplete)
        {
            while (true)
            {
                object current;
                try
                {
                    if (enumerator.MoveNext() == false)
                        break;
                    current = enumerator.Current;
                }
                catch (Exception ex)
                {
                    onComplete(ex);
                    yield break;
                }
                yield return current;
            }
            onComplete(null);
        }
    }

    namespace EnumeratorExtensions
    {
        public static class EnumeratorExtensions
        {
            public static IEnumerator OnException(this IEnumerator enumerator, Action<Exception> callback)
            {
                return CoroutineUtil.WrapThrowingEnumerator(enumerator, e =>
                {
                    if (e != null)
                        callback(e);
                });
            }
        }
    }
}
