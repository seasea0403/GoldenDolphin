using UnityEngine;

namespace LingBoCanteen
{
    /// <summary>
    /// 单例基类：继承自MonoBehaviour的通用单例模板
    /// </summary>
    public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T m_Instance;

        public static T Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = FindObjectOfType<T>();
                    
                    if (m_Instance == null)
                    {
                        GameObject singletonObject = new GameObject(typeof(T).Name);
                        m_Instance = singletonObject.AddComponent<T>();
                        DontDestroyOnLoad(singletonObject);
                    }
                }
                return m_Instance;
            }
        }

        protected virtual void Awake()
        {
            if (m_Instance != null && m_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            m_Instance = (T)(object)this;
            DontDestroyOnLoad(gameObject);
            Init();
        }

        public virtual void Init()
        {
        }
    }
}
