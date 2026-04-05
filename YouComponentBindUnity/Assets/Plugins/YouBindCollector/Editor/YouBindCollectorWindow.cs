using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace YouBindCollector
{
    // 绑定器编辑器代码
    public class YouBindCollectorWindow : EditorWindow
    {
        public int dragStartIndex = -1;
        public int dragEndIndex = -1;
        public BindObjectInfo dragingComponentBindInfo;
        private BindObjectInfo currentBindComponentInfo;
        private Vector2 scrollPosition = Vector2.zero;
        private string searchString = "";
        private BindObjectInfo waitDeleteBindInfo;
        private int operationTabIndex = 0;
        private static readonly string[] OperationTabTitleArray = { "新增组件", "更多功能", "设置" };
        private static readonly Color MissingRefColorDark = new Color(1f, 0.75f, 0.2f, 1f);
        private static readonly Color MissingRefColorLight = new Color(0.75f, 0.2f, 0f, 1f);
        private const float RightPanelWidth = 240f;
        private const float MinWindowWidth = 600f;

        public YouBindCollector rootBindBase
        {
            get { return YouBindCollectorController.Instance.rootBindBase; }
            set { YouBindCollectorController.Instance.SetRootBindBase(value); }
        }

        public List<BindObjectInfo> showComponentBindInfoList = new List<BindObjectInfo>();
        //public List<ComponentBindInfo> showComponentBindInfoList => rootBindBase?.bindInfoList;

        [MenuItem("Tools/组件绑定工具 YouComponentBind")]
        public static void OpenWindow()
        {
            var window = GetWindow<YouBindCollectorWindow>("YouComponentBindWindow");
            window.titleContent = new GUIContent("组件绑定工具 YouComponentBind");
            window.ApplyMinWindowWidth();
        }

        private void Awake()
        {
            ApplyMinWindowWidth();
            OnSelectionChange();
        }

        private void OnEnable()
        {
            ApplyMinWindowWidth();
        }

        private void ApplyMinWindowWidth()
        {
            minSize = new Vector2(MinWindowWidth, minSize.y);
        }

        private void OnSelectionChange()
        {
            var bind = YouBindUtils.GetFirstComponentInParent<YouBindCollector>(Selection.activeTransform);
            if (rootBindBase != bind)
            {
                rootBindBase = bind;
                ApplySearchStr("");
            }

            Repaint();
        }

        // 为了兼容性考虑我们不用ODIN和uiToolKit
        private void OnGUI()
        {
            if (rootBindBase == null)
                OnSelectionChange();

            EditorGUILayout.BeginHorizontal();
            DrawLeftComponentListPanel();
            GUILayout.Space(6f);
            DrawRightToolbarPanel();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLeftComponentListPanel()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUILayout.Label("引用组件列表", EditorStyles.boldLabel);

            if (!rootBindBase)
            {
                EditorGUILayout.HelpBox("请选择带有 YouBindCollector 的目标根物体。", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            ShowSearchGUI();
            ApplySelectFieldName();

            // 显示组件列表
            ResetFieldNameSet();
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
            foreach (var bindInfo in showComponentBindInfoList)
                ShowBindObjectInfo(bindInfo);

            if (waitDeleteBindInfo != null)
            {
                YouBindCollectorController.Instance.RemoveBindComponent(waitDeleteBindInfo);
                waitDeleteBindInfo = null;
            }
            GUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawRightToolbarPanel()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(RightPanelWidth), GUILayout.ExpandHeight(true));
            if (rootBindBase == null)
                EditorGUILayout.ObjectField("目标根物体", null, typeof(GameObject), false);
            else
                EditorGUILayout.ObjectField("目标根物体", rootBindBase.gameObject, typeof(GameObject), false);
            GUILayout.Space(4f);
            DrawOperationPanel();
            EditorGUILayout.EndVertical();
        }

        private void DrawOperationPanel()
        {
            operationTabIndex = GUILayout.Toolbar(operationTabIndex, OperationTabTitleArray);
            if (operationTabIndex < 0 || operationTabIndex >= OperationTabTitleArray.Length)
                operationTabIndex = 0;
            GUILayout.Space(4f);

            switch (operationTabIndex)
            {
                case 0:
                    DrawAddComponentPanel();
                    break;
                case 1:
                    DrawFunctionPanel();
                    break;
                case 2:
                    DrawSettingPanel();
                    break;
                default:
                    DrawAddComponentPanel();
                    break;
            }
        }

        private void DrawAddComponentPanel()
        {
            using (new EditorGUI.DisabledScope(!rootBindBase))
            {
                if (GUILayout.Button("生成代码"))
                {
                    // 生成代码
                    YouBindCodeGenerater.Instance.DoGenerate(rootBindBase);
                }
                var obj = EditorGUILayout.ObjectField("拖入组件手动添加", null, typeof(UnityEngine.Object), true);
                if (obj)
                {
                    // 进行组件增删前重建下缓存
                    YouBindCollectorController.Instance.SetRootBindBase(rootBindBase);
                    YouBindCollectorController.Instance.AddBindComponent(obj);
                    Repaint();
                }
                ShowAppendContent();
            }
        }

        private void DrawFunctionPanel()
        {
            if (GUILayout.Button("新增绑定流程"))
            {
                YouBindCollectorController.Instance.UpdateCode(rootBindBase);
            }

            using (new EditorGUI.DisabledScope(!rootBindBase))
            {
                if (GUILayout.Button("自动扫描"))
                {
                    // 进行组件增删前重建下缓存
                    YouBindCollectorController.Instance.ScanComponent(rootBindBase);
                }

                if (GUILayout.Button("生成代码"))
                {
                    // 生成代码
                    YouBindCodeGenerater.Instance.DoGenerate(rootBindBase);
                }

                if (GUILayout.Button("删除代码"))
                {
                    DeleteCodeFile();
                }
            }
        }

        private void DrawSettingPanel()
        {
            var refreshAfterGenCode = EditorPrefs.GetBool(YouBindGlobalDefine.YouComponentBind_RefreshAfterGenCode, true);
            var newRefreshAfterGenCode = EditorGUILayout.Toggle("更新代码后立即刷新", refreshAfterGenCode);
            if (refreshAfterGenCode != newRefreshAfterGenCode)
            {
                EditorPrefs.SetBool(YouBindGlobalDefine.YouComponentBind_RefreshAfterGenCode, newRefreshAfterGenCode);
            }

            var showHierarchyMark = EditorPrefs.GetBool(YouBindGlobalDefine.YouComponentBind_ShowHierarchyMarkInEditMode, true);
            var newShowHierarchyMark = EditorGUILayout.Toggle("编辑模式显示代码引用红*", showHierarchyMark);
            if (showHierarchyMark != newShowHierarchyMark)
            {
                EditorPrefs.SetBool(YouBindGlobalDefine.YouComponentBind_ShowHierarchyMarkInEditMode, newShowHierarchyMark);
                YouBindHierarchyMark.RefreshDisplaySetting();
            }
        }

        public void ShowBindObjectInfo(BindObjectInfo info)
        {
            currentBindComponentInfo = info;
            GUILayout.BeginHorizontal();
            var oldGenCode = info.genCode;
            info.genCode = GUILayout.Toggle(info.genCode, "", GUILayout.Width(20));
            if (oldGenCode != info.genCode)
            {
                YouBindHierarchyMark.NotifyCollectorChanged(rootBindBase);
            }
            //调试搜索功能
            //GUILayout.Label(info.searchPriority.ToString(), GUILayout.Width(20));
            // 调试类型序列化
            //GUILayout.Label(info.bindType?.Name, GUILayout.Width(50));

            // 显示字段名和快速改字段名支持
            var isNameValid = IsNameValid(info.fieldName);
            var oldColor = GUI.color;
            if (!isNameValid)
                GUI.color = Color.red;
            var newFieldName = EditorGUILayout.TextField(info.fieldName, GUILayout.Width(100));
            GUI.color = oldColor;
            if (newFieldName != info.fieldName)
            {
                var config = YouBindTypeConfigManager.Instance.GetBindConfig(info.bindType);
                Debug.Log("" + info.bindType + config);
                newFieldName = Regex.Replace(newFieldName, "^" + config.prefix, "");
                var transform = YouBindUtils.GetObjectTransform(info.bindObject);
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
            {
                // 引用失效时显示文本提示，避免把字符串当作GUIStyle名称。
                var oldContentColor = GUI.contentColor;
                GUI.contentColor = EditorGUIUtility.isProSkin ? MissingRefColorDark : MissingRefColorLight;
                GUILayout.Label("引用丢失", EditorStyles.boldLabel, GUILayout.Width(200));
                GUI.contentColor = oldContentColor;
            }
            // if (GUILayout.Button("\u00D7", GUILayout.Width(20))) // ×
            // {
            //     waitDeleteBindInfo = info;
            //     return;
            // }

            // 显示事件列表
            // if (GUILayout.Button(info.foldout ? "-" : "+", GUILayout.Width(20))) info.foldout = !info.foldout;
            // var showEventStr = "";
            // if (info?.eventInfoList != null && info.eventInfoList.Count > 0)
            //     showEventStr = string.Concat(
            //         info.eventInfoList.Select(
            //             p => p.eventName + ", "));

            // GUILayout.Label(showEventStr);
            GUILayout.EndHorizontal();

            // if (info.foldout)
            for (var index = 0; index < info.eventInfoList.Count; index++)
                ShowEventBindInfo(index);
        }

        private void ShowEventBindInfo(int index)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(50f);
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
                    YouBindCollectorController.SwapEvent(currentBindComponentInfo, oldDragEndIndex, dragStartIndex);
                    YouBindCollectorController.SwapEvent(currentBindComponentInfo, dragEndIndex, dragStartIndex);
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

        private void ShowAppendContent()
        {
            var go = Selection.activeGameObject;
            if (go == null) return;
            ShowAppendContentItem(go);
            var bindObjectArray = go.GetComponents<Component>();
            foreach (var bindObject in bindObjectArray)
            {
                ShowAppendContentItem(bindObject);
            }
        }

        private void ShowAppendContentItem(UnityEngine.Object bindObject)
        {
            if(bindObject == null) return;
            var bindType = bindObject.GetType();
            var joined = rootBindBase?.joinedObjectSet?.Contains(bindObject);
            if (joined == true)
            {
                var bindInfo = rootBindBase.bindInfoList.Find(p => p.bindObject == bindObject);
                joined = bindInfo?.genCode;
            }
            var showName = (joined == true ? "【已加入】" : "") + bindType.Name;
            if (!GUILayout.Button(showName)) return;
            if (rootBindBase == null)
            {
                Debug.LogError("未找到收集器");
                return;
            }
            if (joined == true)
            {
                YouBindCollectorController.Instance.SetRootBindBase(rootBindBase);
                YouBindCollectorController.Instance.RemoveBindComponent(bindObject);
            }
            else
            {
                YouBindCollectorController.Instance.SetRootBindBase(rootBindBase);
                YouBindCollectorController.Instance.AddBindComponent(bindObject);
            }
            Repaint();
        }

        // 搜索时显示的是一个独立列表，此时如果修改原始数据，列表不会更新
        private void ApplySearchStr(string searchString)
        {
            if (!rootBindBase) return;
            this.searchString = searchString;
            if (string.IsNullOrEmpty(searchString))
            {
                showComponentBindInfoList = rootBindBase.bindInfoList;
                return;
            }

            showComponentBindInfoList = YouBindUtils.SearchSort(
                rootBindBase.bindInfoList, p => p.fieldName, searchString);
            Repaint();
        }

        private void ShowSearchGUI()
        {
            // 字段名搜索和显示选中物体字段名
            EditorGUILayout.BeginHorizontal();
            var newSearchString = EditorGUILayout.TextField("搜索", searchString);
            if (GUILayout.Button("\u00D7", GUILayout.Width(20)))// ×
            {
                newSearchString = "";
            }
            EditorGUILayout.EndHorizontal();
            if (newSearchString != searchString) ApplySearchStr(newSearchString);
        }

        public void ApplySelectFieldName()
        {
            var selectInfo = showComponentBindInfoList.Find(p =>
            {
                var component = p.bindObject;
                var tf = YouBindUtils.GetObjectTransform(component);
                if (tf == Selection.activeTransform)
                    return true;
                return false;
            });
            var showFieldName = selectInfo?.fieldName;
            if (string.IsNullOrEmpty(showFieldName)) showFieldName = "";
            EditorGUILayout.TextField("选中物体", showFieldName);
        }

        #region 字段命名提示
        Dictionary<string, int> fieldNameDict = new Dictionary<string, int>();
        Regex identifierRegex = new Regex(@"^[a-zA-Z_][a-zA-Z0-9_]*$");
        public void ResetFieldNameSet()
        {
            fieldNameDict.Clear();
            foreach (var bindInfo in rootBindBase.bindInfoList)
            {
                int count;
                fieldNameDict.TryGetValue(bindInfo.fieldName, out count);
                fieldNameDict[bindInfo.fieldName] = count + 1;
            }
        }
        public bool IsNameValid(string fieldName)
        {
            var result = identifierRegex.Match(fieldName);
            if (result == null)
                return false;
            int count;
            fieldNameDict.TryGetValue(fieldName, out count);
            if (count > 1)
                return false;
            return result.Success;
        }
        #endregion

        // 清理代码文件，点错的时候用
        public void DeleteCodeFile()
        {
            if (rootBindBase == null)
                return;
            var message = $"警告！将删除生成代码文件，点击确认继续\n" +
                $"将删除：{rootBindBase.targetClassName}.cs\n" +
                $"{rootBindBase.targetClassName}.g.cs";
            if (!EditorUtility.DisplayDialog("确认", message, "确认", "取消"))
            {
                return;
            }
            File.Delete(YouBindGlobalDefine.GetCSharpGenCodeFilePath(rootBindBase.targetClassName));
            File.Delete(YouBindGlobalDefine.GetCSharpCustomCodeFilePath(rootBindBase.targetClassName));
        }
    }
}
