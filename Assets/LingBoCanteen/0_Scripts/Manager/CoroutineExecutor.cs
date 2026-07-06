using System.Collections;
using UnityEngine;

namespace LingBoCanteen
{
    /// <summary>
    /// 协程执行器：为非MonoBehaviour对象提供协程支持
    /// </summary>
    public class CoroutineExecutor : MonoSingleton<CoroutineExecutor>
    {
        /// <summary>
        /// 执行协程
        /// </summary>
        public void ExecuteCoroutine(IEnumerator coroutine)
        {
            StartCoroutine(coroutine);
        }
    }
}
