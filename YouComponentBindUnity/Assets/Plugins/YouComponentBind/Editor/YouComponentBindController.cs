using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace YouComponentBind
{
    // 绑定器功能代码
    public class YouComponentBindController
    {
        public static YouComponentBindController Instance { get; private set; } = new YouComponentBindController();
        public YouBindBase rootBindBase { get; private set; }

        public void SetRootBindBase(YouBindBase newBindBase)
        {
            rootBindBase = newBindBase;
            joinedObjectSet.Clear();
            resultList.ForEach(p =>
            {
                if (p?.bindObject != null) joinedObjectSet.Add(p.bindObject);
            });
        }

        // 简单模糊搜索算法
        // pattern以空格分割为若干匹配词，分别匹配，返回匹配成功的子串数量。
        // 如果匹配词是input的子序列，视为匹配成功。
        public static int Search(string input, string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return 1;
            input = input.ToLower();
            pattern = pattern.ToLower();
            var ans = 0;
            var splitedPattern = pattern.Split(' ');
            foreach (var key in splitedPattern)
            {
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

        #region MyRegion 自动扫描组件，添加需要默认生成的组件
        private List<ComponentBindInfo> resultList => rootBindBase?.bindInfoList;
        private readonly HashSet<Object> joinedObjectSet = new HashSet<Object>();

        public void ScanComponent()
        {
            var root = rootBindBase.transform;
            ScanComponentHelp(root);
        }

        // 一边扫描一边生成路径
        public void ScanComponentHelp(Transform root)
        {
            // Debug.Log("扫描节点"+relativePath);
            var componentArray = root.GetComponents<Component>();
            var arrayLength = componentArray.Length;
            for (var i = 0; i < arrayLength; i++) AddBindComponent(componentArray[i]);

            var childCount = root.childCount;
            for (var i = 0; i < childCount; i++) ScanComponentHelp(root.GetChild(i));
        }
        #endregion

        // 添加单个组件
        public void AddBindComponent(Object bindObject, bool enableNoConfig = false,
            bool showMessage = false)
        {
            if (!bindObject) return;
            var type = bindObject.GetType();
            var bindConfig = YouBindConfigManager.Instance.GetBindConfig(type);
            if (!enableNoConfig && bindConfig?.autoBind != true)
                return;
            if (joinedObjectSet.Contains(bindObject))
            {
                if (showMessage)
                    Debug.LogError("此物体已经被添加到绑定列表中了" + bindObject, bindObject);
                return;
            }

            Debug.Log("添加组件" + type);
            var objectTF = GetObjectTransform(bindObject);
            var relativePath = GetRelativePath(objectTF);
            var componentInfo = new ComponentBindInfo()
            {
                genCode = true,
                relativePath = relativePath,
                bindObject = bindObject,
                bindType = type,
                eventInfoList = new List<BindEventInfo>(),
            };
            componentInfo.fieldName = GetFieldName(componentInfo, bindConfig, objectTF);
            AddComponentDefaultEvent(componentInfo);

            resultList.Add(componentInfo);
            joinedObjectSet.Add(bindObject);
        }

        // 为组件添加默认事件
        public void AddComponentDefaultEvent(ComponentBindInfo componentInfo)
        {
            var bindConfig = YouBindConfigManager.Instance.GetBindConfig(componentInfo.bindType);
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

        // 如果想要修改字段命名，请修改此函数
        public string GetFieldName(ComponentBindInfo bindInfo, YouComponentBindConfig bindConfig = null, Transform objectTF = null)
        {
            if (bindInfo == null)
                return "";
            if (bindConfig == null)
                bindConfig = YouBindConfigManager.Instance.GetBindConfig(bindInfo.bindType);
            if (objectTF == null)
                objectTF = GetObjectTransform(bindInfo.bindObject);
            return bindConfig?.prefix + objectTF?.name;
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

    }
}