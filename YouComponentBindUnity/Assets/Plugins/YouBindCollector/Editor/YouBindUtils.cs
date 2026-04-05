using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace YouBindCollector
{
    public static class YouBindUtils
    {
        public static string GetRelativePath(Transform tf, Transform root = null)
        {
            if (tf == null) return "";
            if (root == null)
                root = YouBindCollectorController.Instance.rootBindBase.transform;

            var ans = new StringBuilder();
            while (tf != null && tf != root)
            {
                if (ans.Length > 0)
                    ans.Insert(0, "/");
                ans.Insert(0, tf.name);
                tf = tf.parent;
            }

            if (tf == null) return "";
            return ans.ToString();
        }

        // 获取Object的transform。如果不是gameobject也不是component，返回空。
        public static Transform GetObjectTransform(Object targetObject)
        {
            if (targetObject == null) return null;

            var component = targetObject as Component;
            if (component != null) return component.transform;

            var gameObject = targetObject as GameObject;
            if (gameObject != null) return gameObject.transform;

            return null;
        }

        // 按照遍历父物体的顺序查找组件，包括自己的组件
        public static T GetFirstComponentInParent<T>(Transform target) where T : YouBindCollector
        {
            while (target != null)
            {
                var bindBase = target.GetComponent<T>();
                if (bindBase)
                    return bindBase;
                target = target.parent;
            }

            return null;
        }

        // 简单模糊搜索算法
        // pattern以空格分割为若干匹配词，分别匹配，返回匹配成功的子串数量。
        // 如果匹配词是input的子序列，视为匹配成功。
        public static List<T> SearchSort<T>(IEnumerable<T> items,
            Func<T, string> getWord, string pattern) where T : BindObjectInfo
        {
            return items.Select(data =>
            {
                var word = getWord(data);
                var priority = GetSearchPriority(word, pattern);
                return new { data, priority, word.Length };
            })
                .Where(p => p.priority > 0)
                .OrderBy(p => -p.priority)
                .ThenBy(p => p.Length)
                .Select(p => p.data)
                .ToList();
        }

        public static int GetSearchPriority(string input, string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return 1;

            input = input.ToLower();
            pattern = pattern.ToLower();
            var ans = 0;
            var splitedPattern = pattern.Split(' ');
            foreach (var key in splitedPattern)
            {
                if (string.IsNullOrWhiteSpace(key))
                    continue;
                int a = 0;
                int b = 0;
                while (a < input.Length && b < key.Length)
                {
                    if (input[a] == key[b])
                        b++;
                    a++;
                }
                if (b == key.Length)
                    ans++;
            }
            return ans;
        }
    }
}
