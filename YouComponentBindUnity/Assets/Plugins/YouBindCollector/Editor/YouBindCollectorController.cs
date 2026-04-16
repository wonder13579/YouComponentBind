using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            var objectTF = YouBindUtils.GetObjectTransform(bindObject);
            var relativePath = YouBindUtils.GetRelativePath(objectTF);
            var componentInfo = new BindObjectInfo()
            {
                genCode = true,
                joinIndex = GetNextJoinIndex(resultList),
                relativePath = relativePath,
                bindObject = bindObject,
                bindType = type,
                eventInfoList = new List<BindEventInfo>(),
            };
            componentInfo.fieldName = YouBindGlobalDefine.GetFieldName(componentInfo, objectTF);
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
            if (info == null) return;
            info.genCode = false;
            SortBindObjectInfo(rootBindBase);
            YouBindHierarchyMark.NotifyCollectorChanged(rootBindBase);
        }

        public void RemoveBindComponentPermanently(BindObjectInfo info)
        {
            if (info == null || rootBindBase == null || resultList == null)
                return;

            resultList.Remove(info);
            rootBindBase.joinedObjectSet.Clear();
            SortBindObjectInfo(rootBindBase);
            EditorUtility.SetDirty(rootBindBase);
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

        public YouBindCollector CreateBindCollector(YouBindCollector collector = null)
        {
            bool _;
            return CreateBindCollector(out _, collector);
        }

        public YouBindCollector CreateBindCollector(out bool createdNewCollector, YouBindCollector collector = null)
        {
            createdNewCollector = false;
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
            createdNewCollector = true;
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
                        view = collector.gameObject.AddComponent(compType);
                    return view;
                }
            }
            return null;
        }

        public Component AddCommonLuaView(YouBindCollector collector = null)
        {
            if (collector == null)
                return null;

            const string luaViewTypeName = "CommonLuaView";
            var assembArray = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assemb in assembArray)
            {
                var compType = assemb.GetType(luaViewTypeName);
                if (compType == null)
                    continue;
                if (!typeof(Component).IsAssignableFrom(compType))
                    continue;

                var view = collector.gameObject.GetComponent(compType);
                if (view == null)
                    view = collector.gameObject.AddComponent(compType);
                return view;
            }

            Debug.LogWarning($"未找到组件类型 {luaViewTypeName}，请检查脚本是否存在。", collector);
            return null;
        }

        // 计划将这里做成一键的，用户只点这个按钮就行
        public void UpdateCode(YouBindCollector bind = null)
        {
            // 添加收集器
            bool createdNewCollector;
            bind = CreateBindCollector(out createdNewCollector, bind);
            if (bind == null) return;

            SetRootBindBase(bind);
            // 扫描组件
            ScanComponent(bind);
            if (createdNewCollector)
            {
                Debug.Log("首次新增绑定流程：已自动扫描组件，请确认引用后再次点击生成代码。", bind);
                return;
            }
            // 生成代码
            YouBindCodeGenerater.Instance.DoGenerate(rootBindBase);
            if (rootBindBase.codeGenerateType != YouBindCollector.CodeGenerateType.CSharp)
                return;
            // 添加绑定组件
            var view = AddBindview(bind);
            TryInitializeView(view);
        }

        internal static void TryInitializeView(Component view)
        {
            if (view == null)
                return;

            var methodInfo = view.GetType().GetMethod("InitializeView", BindingFlags.Public | BindingFlags.Instance);
            if (methodInfo != null)
                methodInfo.Invoke(view, null);
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
                    EnsureJoinOrderIndex(collector.bindInfoList);
                    collector.bindInfoList = collector.bindInfoList
                        .OrderBy(p => p.joinIndex)
                        .ToList();
                    break;
                case YouBindCollector.SortOrder.Custom:
                    break;
                default:
                    break;
            }
        }

        public void SortBindObjectInfoByJoinOrder(YouBindCollector collector)
        {
            if (collector == null || collector.bindInfoList == null) return;
            EnsureJoinOrderIndex(collector.bindInfoList);
            collector.bindInfoList = collector.bindInfoList
                .OrderBy(p => p.joinIndex)
                .ToList();
        }

        private static void EnsureJoinOrderIndex(List<BindObjectInfo> bindInfoList)
        {
            if (bindInfoList == null || bindInfoList.Count <= 0) return;

            var usedIndexSet = new HashSet<int>();
            var needRebuild = false;
            for (var i = 0; i < bindInfoList.Count; i++)
            {
                var info = bindInfoList[i];
                if (info == null) continue;

                if (info.joinIndex < 0 || !usedIndexSet.Add(info.joinIndex))
                {
                    needRebuild = true;
                    break;
                }
            }

            if (!needRebuild) return;

            var joinIndex = 0;
            for (var i = 0; i < bindInfoList.Count; i++)
            {
                var info = bindInfoList[i];
                if (info == null) continue;
                info.joinIndex = joinIndex;
                joinIndex++;
            }
        }

        private static int GetNextJoinIndex(List<BindObjectInfo> bindInfoList)
        {
            if (bindInfoList == null || bindInfoList.Count <= 0) return 0;

            EnsureJoinOrderIndex(bindInfoList);
            var maxJoinIndex = -1;
            for (var i = 0; i < bindInfoList.Count; i++)
            {
                var info = bindInfoList[i];
                if (info == null) continue;
                if (info.joinIndex > maxJoinIndex)
                    maxJoinIndex = info.joinIndex;
            }

            return maxJoinIndex + 1;
        }
    }
}
