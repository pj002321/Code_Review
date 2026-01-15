using UnityEngine;

namespace Hunt
{
    public static class ObjectExtension
    {
        public static T ValidInit<T>(this T obj, string name = null) where T : class
        {
            string componentName = name ?? typeof(T).Name;

            if (obj == null)
            {
                $"{componentName}이(가) null입니다.".DError();
            }
            else
            {
                $"[Init] {componentName} 초기화".DLog();
            }

            return obj;
        }
        public static bool IsNull<T>(this T obj, string name = null) where T : class
        {
            if (obj == null)
            {
                string componentName = name ?? typeof(T).Name;
                $"{componentName}이(가) null입니다.".DError();
                return true;
            }
            return false;
        }
    }


}