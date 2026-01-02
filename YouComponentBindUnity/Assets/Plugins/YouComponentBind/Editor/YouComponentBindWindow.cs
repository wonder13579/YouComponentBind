using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace YouComponentBind
{
    // 绑定器编辑器代码
    public class YouComponentBindWindow : EditorWindow
    {
        public int dragStartIndex = -1;
        public int dragEndIndex = -1;
        public ComponentBindInfo dragingComponentBindInfo;
        private ComponentBindInfo currentBindComponentInfo;
        private Vector2 scrollPosition = Vector2.zero;
        private string searchString = "";

        public YouBindBase rootBindBase
        {
            get { return YouComponentBindController.Instance.rootBindBase; }
            set { YouComponentBindController.Instance.SetRootBindBase(value); }
        }

        public List<ComponentBindInfo> showComponentBindInfoList = new List<ComponentBindInfo>();
        //public List<ComponentBindInfo> showComponentBindInfoList => rootBindBase?.bindInfoList;

        public static void OpenWindow()
        {
            var window = GetWindow<YouComponentBindWindow>("YouComponentBindWindow");
        }

        private void Awake()
        {
            OnSelectionChange();
        }

        private void OnSelectionChange()
        {
            var newBase = GetFirstComponentInParent<YouBindBase>(Selection.activeTransform);
            if (rootBindBase != newBase)
            {
                rootBindBase = newBase;
                ApplySearchStr("");
            }

            Repaint();
        }


        // 为了兼容性考虑我们不用ODIN和uiToolKit
        private void OnGUI()
        {
            if (rootBindBase == null)
                OnSelectionChange();
            EditorGUILayout.ObjectField("目标根物体", rootBindBase?.gameObject, typeof(GameObject), false);
            if (!rootBindBase)
                return;

            if (GUILayout.Button("自动扫描"))
            {
                // 进行组件增删前重建下缓存
                YouComponentBindController.Instance.SetRootBindBase(rootBindBase);
                YouComponentBindController.Instance.ScanComponent();
            }
            var obj = EditorGUILayout.ObjectField("拖入组件手动添加", null, typeof(Object), true);
            if (obj)
            {
                // 进行组件增删前重建下缓存
                YouComponentBindController.Instance.SetRootBindBase(rootBindBase);
                YouComponentBindController.Instance.AddBindComponent(obj, enableNoConfig: true, showMessage: true);
                Repaint();
            }

            EditorGUILayout.BeginHorizontal();
            // 字段名搜索和显示选中物体字段名
            var newSearchString = EditorGUILayout.TextField("搜索", searchString);
            if (GUILayout.Button("\u00D7", GUILayout.Width(20)))// ×
            {
                newSearchString = "";
            }
            EditorGUILayout.EndHorizontal();
            if (newSearchString != searchString) ApplySearchStr(newSearchString);
            ApplySelectFieldName();

            // 显示组件列表
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            foreach (var bindInfo in showComponentBindInfoList) ShowComponentBindInfo(bindInfo);

            GUILayout.EndScrollView();
        }

        public void ShowComponentBindInfo(ComponentBindInfo info)
        {
            currentBindComponentInfo = info;
            GUILayout.BeginHorizontal();
            info.genCode = GUILayout.Toggle(info.genCode, "", GUILayout.Width(20));
            // 调试搜索功能
            // GUILayout.Label(info.commonSubsequenceCount.ToString(), GUILayout.Width(20));
            // 调试类型序列化
            //GUILayout.Label(info.bindType?.Name, GUILayout.Width(50));

            // 显示字段名和快速改字段名支持
            var newFieldName = EditorGUILayout.TextField(info.fieldName, GUILayout.Width(100));
            if (newFieldName != info.fieldName)
            {
                var config = YouBindConfigManager.Instance.GetBindConfig(info.bindType);
                Debug.Log("" + info.bindType + config);
                newFieldName = Regex.Replace(newFieldName, "^" + config.prefix, "");
                var transform = YouComponentBindController.GetObjectTransform(info.bindObject);
                transform.name = newFieldName;
                info.fieldName = config.prefix + newFieldName;
            }

            // 显示引用
            // 显示go或者component，用户不用关心他的类型
            if (info.bindObject as Component)
                EditorGUILayout.ObjectField(info.bindObject, typeof(Component), false, GUILayout.Width(200));
            else if (info.bindObject as GameObject)
                EditorGUILayout.ObjectField(info.bindObject, typeof(GameObject), false, GUILayout.Width(200));
            else
                // Debug.LogError("引用丢失");
                GUILayout.Label("", "引用丢失", GUILayout.Width(200));

            // 显示事件列表
            if (GUILayout.Button(info.foldout ? "-" : "+", GUILayout.Width(20))) info.foldout = !info.foldout;
            var showEventStr = "";
            if (info?.eventInfoList != null && info.eventInfoList.Count > 0)
                showEventStr = string.Concat(
                    info.eventInfoList.Select(
                        p => p.eventName + ", "));

            GUILayout.Label(showEventStr);
            GUILayout.EndHorizontal();

            if (info.foldout)
                for (var index = 0; index < info.eventInfoList.Count; index++)
                    ShowEventBindInfo(index);
        }

        private void ShowEventBindInfo(int index)
        {
            GUILayout.BeginHorizontal();
            var eventInfo = currentBindComponentInfo.eventInfoList[index];
            var eventName = eventInfo.eventName;
            var showNameStr = eventName;
            if (dragingComponentBindInfo != currentBindComponentInfo || index == dragEndIndex || dragEndIndex == -1)
                showNameStr += "\u2195"; //↕
            eventInfo.genCode = EditorGUILayout.Toggle(eventInfo.genCode, GUILayout.Width(20));
            var itemRect = GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true));
            EditorGUI.LabelField(itemRect, showNameStr);
            GUILayout.EndHorizontal();


            if (!itemRect.Contains(Event.current.mousePosition))
                return;
            // 拖动排序处理
            var oldDragEndIndex = dragEndIndex;
            if (Event.current.type == EventType.MouseDown)
            {
                dragStartIndex = index;
                dragEndIndex = index;
                dragingComponentBindInfo = currentBindComponentInfo;
                Repaint();
            }
            else if (Event.current.type == EventType.MouseDrag)
            {
                dragEndIndex = index;
                if (oldDragEndIndex != dragEndIndex)
                {
                    SwapEvent(oldDragEndIndex, dragStartIndex);
                    SwapEvent(dragEndIndex, dragStartIndex);
                    Repaint();
                }
            }
            else if (Event.current.type == EventType.MouseUp)
            {
                dragStartIndex = -1;
                dragEndIndex = -1;
                dragingComponentBindInfo = null;
                Repaint();
            }
        }

        private void SwapEvent(int indexA, int indexB)
        {
            var eventCount = currentBindComponentInfo?.eventInfoList?.Count;
            if (eventCount == null || eventCount <= 0)
                return;
            if (indexA == indexB)
                return;
            if (indexA < 0 || eventCount <= indexA)
                return;
            if (indexB < 0 || eventCount <= indexB)
                return;
            var list = currentBindComponentInfo.eventInfoList;
            var temp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = temp;
        }

        // 搜索时显示的是一个独立列表，此时如果修改原始数据，列表不会更新
        private void ApplySearchStr(string searchString)
        {
            this.searchString = searchString;
            if (string.IsNullOrEmpty(searchString))
            {
                showComponentBindInfoList = rootBindBase.bindInfoList;
                return;
            }

            showComponentBindInfoList = new List<ComponentBindInfo>();
            for (var i = 0; i < rootBindBase.bindInfoList.Count; i++)
            {
                var info = rootBindBase.bindInfoList[i];
                info.searchPriority = YouComponentBindController.Search(
                    info.fieldName, searchString);
                if (info.searchPriority > 0) showComponentBindInfoList.Add(info);
            }

            showComponentBindInfoList.Sort((a, b) =>
                b.searchPriority - a.searchPriority);
            Repaint();
        }

        public void ApplySelectFieldName()
        {
            var selectInfo = showComponentBindInfoList.Find(p =>
            {
                var component = p.bindObject;
                var tf = YouComponentBindController.GetObjectTransform(component);
                if (tf == Selection.activeTransform)
                    return true;
                return false;
            });
            var showFieldName = selectInfo?.fieldName;
            if (string.IsNullOrEmpty(showFieldName)) showFieldName = "";
            EditorGUILayout.TextField("选中物体", showFieldName);
        }

        // 按照遍历父物体的顺序查找组件，包括自己的组件
        public static T GetFirstComponentInParent<T>(Transform target) where T : YouBindBase
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
    }
}