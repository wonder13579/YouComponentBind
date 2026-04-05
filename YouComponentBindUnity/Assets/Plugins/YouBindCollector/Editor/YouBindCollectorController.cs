using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace YouBindCollector
{
    // 绑定器功能代码
    public class YouBindCollectorController
    {
        public static YouBindCollectorController Instance { get; private set; } = new YouBindCollectorController();
        public YouBindCollector rootBindBase { get; private set; }

        public void SetRootBindBase(YouBindCollector newBindBase)
        {
            if (rootBindBase == newBindBase) return;
            rootBindBase = newBindBase;
        }

        #region MyRegion 自动扫描组件，添加需要默认生成的组件
        private List<BindObjectInfo> resultList => rootBindBase?.bindInfoList;

        public void ScanComponent(YouBindCollector rootBindBase)
        {
            if (string.IsNullOrEmpty(rootBindBase.targetClassName))
            {
                rootBindBase.targetClassName = YouBindGlobalDefine.GetTargetClassName(rootBindBase);
            }
            SetRootBindBase(rootBindBase);
            var root = rootBindBase.transform;
            ScanComponentHelp(root);

            // 重建缓存
            rootBindBase.joinedObjectSet.Clear();
            SortBindObjectInfo(rootBindBase);
            YouBindHierarchyMark.NotifyCollectorChanged(rootBindBase);
        }

        // 一边扫描一边生成路径
        private void ScanComponentHelp(Transform root)
        {
            // Debug.Log("扫描节点"+relativePath);
            var componentArray = root.GetComponents<Component>();
            var arrayLength = componentArray.Length;
            for (var i = 0; i < arrayLength; i++)
                AddBindComponent(componentArray[i], true);

            var childCount = root.childCount;
            for (var i = 0; i < childCount; i++)
                ScanComponentHelp(root.GetChild(i));
        }
        #endregion

        // 添加单个组件
        public void AddBindComponent(Object bindObject,
            bool fromAutoScan = false)
        {
            if (!bindObject) return;
            var type = bindObject.GetType();
            var bindConfig = YouBindTypeConfigManager.Instance.GetBindConfig(type);
            if (fromAutoScan && bindConfig?.autoBind != true)
                return;
            if (rootBindBase.joinedObjectSet.Contains(bindObject))
            {
                if (fromAutoScan)
                    return;
                var bindInfo = resultList.Find(p => p.bindObject == bindObject);
                if (bindInfo?.genCode == false)
                {
                    Debug.Log("打开组件的代码生成" + type);
                    bindInfo.genCode = true;
                    YouBindHierarchyMark.NotifyCollectorChanged(rootBindBase);
                    return;
                }
                Debug.LogError("此物体已经被添加到绑定列表中了" + bindObject, bindObject);
                return;
            }

            Debug.Log("添加组件" + type);
            var objectTF = GetObjectTransform(bindObject);
            var relativePath = GetRelativePath(objectTF);
            var componentInfo = new BindObjectInfo()
            {
                genCode = true,
                relativePath = relativePath,
                bindObject = bindObject,
                bindType = type,
                eventInfoList = new List<BindEventInfo>(),
            };
            componentInfo.fieldName = YouBindGlobalDefine.GetFieldName(componentInfo, bindConfig, objectTF);
            AddComponentDefaultEvent(componentInfo);

            resultList.Add(componentInfo);
            rootBindBase.joinedObjectSet.Add(bindObject);

            if (!fromAutoScan)
            {
                SortBindObjectInfo(rootBindBase);
                YouBindHierarchyMark.NotifyCollectorChanged(rootBindBase);
            }
        }

        public void RemoveBindComponent(Object bindObject)
        {
            if (!bindObject || Instance == null) return;
            var bindInfo = resultList.Find(p => p?.bindObject == bindObject);
            RemoveBindComponent(bindInfo);
        }

        public void RemoveBindComponent(BindObjectInfo info)
        {
            if (info?.bindObject == null) return;
            bool isAutoBind = false;
            var type = info.bindObject.GetType();
            var bindConfig = YouBindTypeConfigManager.Instance.GetBindConfig(type);
            if (bindConfig != null)
            {
                isAutoBind = bindConfig.autoBind;
            }
            if (isAutoBind)
            {
                info.genCode = false;
            }
            else
            {
                rootBindBase.bindInfoList.Remove(info);
                rootBindBase.joinedObjectSet.Remove(info.bindObject);
            }
            SortBindObjectInfo(rootBindBase);
            YouBindHierarchyMark.NotifyCollectorChanged(rootBindBase);
        }

        // 为组件添加默认事件
        public void AddComponentDefaultEvent(BindObjectInfo componentInfo)
        {
            var bindConfig = YouBindTypeConfigManager.Instance.GetBindConfig(componentInfo.bindType);
            if (bindConfig?.eventArray == null)
                return;
            for (var i = 0; i < bindConfig.eventArray.Length; i++)
            {
                var eventConfig = bindConfig.eventArray[i];
                if (!eventConfig.autoGenerate)
                    continue;
                var eventInfo = new BindEventInfo
                {
                    genCode = true,
                    eventName = eventConfig.eventName
                };
                componentInfo.eventInfoList.Add(eventInfo);
            }
        }

        public static string GetRelativePath(Transform tf, Transform root = null)
        {
            if (tf == null) return "";
            if (root == null)
                root = Instance.rootBindBase.transform;
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
            return (targetObject as Component)?.transform ?? (targetObject as GameObject)?.transform;
        }

        public static void SwapEvent(BindObjectInfo info, int indexA, int indexB)
        {
            var eventCount = info?.eventInfoList?.Count;
            if (eventCount == null || eventCount <= 0)
                return;
            if (indexA == indexB)
                return;
            if (indexA < 0 || eventCount <= indexA)
                return;
            if (indexB < 0 || eventCount <= indexB)
                return;
            var list = info.eventInfoList;
            var temp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = temp;
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
        public static List<T> SearchSort<T>(IEnumerable<T> items, Func<T, string> GetWord, string pattern) where T : BindObjectInfo
        {
            return items.Select(data =>
            {
                var word = GetWord(data);
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

        public YouBindCollector CreateBindCollector(YouBindCollector collector = null)
        {
            // 创建绑定配置
            if (collector != null)
                return collector;
            // 压根没有，我们给他创建一个
            if (!Selection.activeGameObject)
            {
                Debug.LogError("请先选择一个gameObject");
                return null;
            }
            // 选中物体已经添加过了，直接返回
            var existCollector = Selection.activeGameObject.GetComponent<YouBindCollector>();
            if (existCollector)
                return existCollector;
            collector = Selection.activeGameObject.AddComponent<YouBindCollector>();
            Debug.Log("已新增绑定配置", collector);
            return collector;
        }

        public Component AddBindview(YouBindCollector collector = null)
        {
            // 检查是否挂了组件，没有给他挂一个
            var typeName = rootBindBase.targetClassName;
            var assembArray = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assemb in assembArray)
            {
                var compType = assemb.GetType(typeName);
                if (compType != null)
                {
                    var view = collector.gameObject.GetComponent(compType);
                    if (view == null)
                        collector.gameObject.AddComponent(compType);
                    return view;
                }
            }
            return null;
        }

        // 计划将这里做成一键的，用户只点这个按钮就行
        public void UpdateCode(YouBindCollector bind = null)
        {
            // 添加收集器
            bind = CreateBindCollector(bind);
            if (bind == null) return;

            SetRootBindBase(bind);
            // 扫描组件
            ScanComponent(bind);
            // 生成代码
            YouBindCodeGenerater.Instance.DoGenerate(rootBindBase);
            // 添加绑定组件
            var view = AddBindview(bind);
            // 重新序列化
            if (view != null)
            {
                MethodInfo methodInfo = view.GetType().GetMethod("InitializeView", BindingFlags.Public | BindingFlags.Instance);
                if (methodInfo != null)
                    methodInfo.Invoke(view, null);
            }
        }

        public void SortBindObjectInfo(YouBindCollector collector)
        {
            SetRootBindBase(collector);
            switch (collector.sortOrder)
            {
                case YouBindCollector.SortOrder.TypeAndName:
                    collector.bindInfoList = collector.bindInfoList
                        .OrderBy(p => p.bindType.Name)
                        .ThenBy(p => p.fieldName)
                        .ToList();
                    break;
                case YouBindCollector.SortOrder.Name:
                    collector.bindInfoList = collector.bindInfoList
                        .OrderBy(p => p.fieldName)
                        .ToList();
                    break;
                case YouBindCollector.SortOrder.JoinOrder:
                    break;
                case YouBindCollector.SortOrder.Custom:
                    break;
                default:
                    break;
            }
        }
    }
}
