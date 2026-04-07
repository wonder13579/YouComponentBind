using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace YouBindCollector
{
    // 处理通用错误检查与自动修复逻辑
    public class YouBindCommonCheckerManager
    {
        public YouBindCollector rootBindBase;
        public List<CheckerErrorBase> errorList = new();

        private Vector2 scrollPosition = Vector2.zero;
        private readonly CheckNodePathChange checkNodePathChange = new();
        private readonly CheckMissingReference checkMissingReference = new();
        private readonly CheckViewNullNode checkViewNullNode = new();

        public void DoAllCheck(YouBindCollector rootBindBase = null)
        {
            if (rootBindBase == null)
                rootBindBase = YouBindCollectorController.Instance.rootBindBase;

            this.rootBindBase = rootBindBase;
            var newErrorList = new List<CheckerErrorBase>();
            checkNodePathChange.Check(rootBindBase, newErrorList);
            checkMissingReference.Check(rootBindBase, newErrorList);
            checkViewNullNode.Check(rootBindBase, newErrorList);
            errorList = newErrorList;
        }

        public void OnGUI()
        {
            DoAllCheck();

            if (rootBindBase == null)
            {
                EditorGUILayout.HelpBox("请先选择带有 YouBindCollector 的目标根物体。", MessageType.Info);
                return;
            }

            if (errorList.Count <= 0)
            {
                EditorGUILayout.HelpBox("未发现错误。", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
            for (var i = 0; i < errorList.Count; i++)
            {
                var error = errorList[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();

                DrawRefObjectField(error.refObject);
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(error.autoFixFunc == null))
                {
                    if (GUILayout.Button("快速修复", GUILayout.Width(70f)))
                    {
                        error.autoFixFunc?.Invoke();
                    }
                }

                EditorGUILayout.EndHorizontal();

                // 错误信息单独占一行，自动换行并自适应高度
                EditorGUILayout.HelpBox(error.message ?? string.Empty, MessageType.None);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }

        private static void DrawRefObjectField(Object refObject)
        {
            if (refObject is Component)
                EditorGUILayout.ObjectField(refObject, typeof(Component), false, GUILayout.Width(140f));
            else if (refObject is GameObject)
                EditorGUILayout.ObjectField(refObject, typeof(GameObject), false, GUILayout.Width(140f));
            else
                EditorGUILayout.ObjectField(refObject, typeof(Object), false, GUILayout.Width(140f));
        }
    }

    // 单条错误信息
    public class CheckerErrorBase
    {
        public string message;
        public Object refObject;
        public Func<bool> autoFixFunc;
    }

    // 检查记录路径和当前路径是否一致
    public class CheckNodePathChange
    {
        public void Check(YouBindCollector collector, List<CheckerErrorBase> errorList)
        {
            if (collector == null || errorList == null) return;
            if (collector.bindInfoList == null || collector.bindInfoList.Count <= 0) return;

            var root = collector.transform;
            for (var i = 0; i < collector.bindInfoList.Count; i++)
            {
                var bindInfo = collector.bindInfoList[i];
                if (bindInfo?.bindObject == null) continue;

                var tf = bindInfo.GetTransform();
                if (tf == null) continue;

                var savedPath = bindInfo.relativePath ?? string.Empty;
                var currentPath = YouBindUtils.GetRelativePath(tf, root);
                if (savedPath == currentPath) continue;

                errorList.Add(new CheckerErrorBase
                {
                    refObject = bindInfo.bindObject,
                    message = $"路径被修改，需要更新代码: {savedPath} -> {currentPath}",
                    autoFixFunc = () => AutoFixPathChange(collector)
                });
            }
        }

        private static bool AutoFixPathChange(YouBindCollector collector)
        {
            if (collector == null) return false;
            if (collector.bindInfoList == null) return false;

            // 先重新生成代码，再更新全部引用组件的记录路径
            YouBindCodeGenerater.Instance.DoGenerate(collector);

            var root = collector.transform;
            Undo.RecordObject(collector, "YouBindCollector Fix Path Change");
            for (var i = 0; i < collector.bindInfoList.Count; i++)
            {
                var info = collector.bindInfoList[i];
                if (info == null) continue;

                var tf = info.GetTransform();
                info.relativePath = tf == null ? string.Empty : YouBindUtils.GetRelativePath(tf, root);
            }

            EditorUtility.SetDirty(collector);
            return true;
        }
    }

    // 检查引用是否丢失（bindObject为空）
    public class CheckMissingReference
    {
        public void Check(YouBindCollector collector, List<CheckerErrorBase> errorList)
        {
            if (collector == null || errorList == null) return;
            if (collector.bindInfoList == null || collector.bindInfoList.Count <= 0) return;

            for (var i = 0; i < collector.bindInfoList.Count; i++)
            {
                var bindInfo = collector.bindInfoList[i];
                if (bindInfo == null) continue;
                if (bindInfo.bindObject != null) continue;

                var bindTypeName = bindInfo.bindType?.Name ?? "UnknownType";
                var savedPath = bindInfo.relativePath ?? string.Empty;
                errorList.Add(new CheckerErrorBase
                {
                    refObject = collector.gameObject,
                    message = $"引用丢失，可尝试重新查找: 字段[{bindInfo.fieldName}] 类型[{bindTypeName}] 路径[{savedPath}]",
                    autoFixFunc = () => TryRestoreBySavedPath(collector, bindInfo)
                });
            }
        }

        private static bool TryRestoreBySavedPath(YouBindCollector collector, BindObjectInfo bindInfo)
        {
            if (collector == null || bindInfo == null)
            {
                Debug.LogWarning("快速修复失败：collector 或 bindInfo 为空。");
                return false;
            }

            var savedPath = bindInfo.relativePath ?? string.Empty;
            var root = collector.transform;
            Transform targetTf;
            if (string.IsNullOrEmpty(savedPath))
                targetTf = root;
            else
                targetTf = root.Find(savedPath);

            if (targetTf == null)
            {
                Debug.LogWarning($"快速修复失败：找不到保存路径对应的节点。可尝试将新组件拖入修复。路径={savedPath}", collector);
                return false;
            }

            var bindType = bindInfo.bindType;
            if (bindType == null)
            {
                Debug.LogWarning($"快速修复失败：字段类型为空。可尝试将新组件拖入修复。字段={bindInfo.fieldName}", collector);
                return false;
            }

            Object resolvedObject = null;
            if (bindType == typeof(GameObject))
                resolvedObject = targetTf.gameObject;
            else if (bindType == typeof(Transform))
                resolvedObject = targetTf;
            else if (bindType == typeof(RectTransform))
                resolvedObject = targetTf as RectTransform;
            else if (typeof(Component).IsAssignableFrom(bindType))
                resolvedObject = targetTf.GetComponent(bindType);
            else
            {
                Debug.LogWarning($"快速修复失败：不支持的引用类型。类型={bindType}", collector);
                return false;
            }

            if (resolvedObject == null)
            {
                Debug.LogWarning($"快速修复失败：路径存在，但缺少对应组件。路径={savedPath} 类型={bindType}", targetTf);
                return false;
            }

            Undo.RecordObject(collector, "YouBindCollector Restore Missing Reference");
            bindInfo.bindObject = resolvedObject;
            EditorUtility.SetDirty(collector);
            return true;
        }
    }

    // 检查生成组件的 view 中是否存在空引用字段
    public class CheckViewNullNode
    {
        public void Check(YouBindCollector collector, List<CheckerErrorBase> errorList)
        {
            if (collector == null || errorList == null) return;
            if (collector.gameObject == null) return;
            if (string.IsNullOrEmpty(collector.targetClassName)) return;

            if (collector.codeGenerateType == YouBindCollector.CodeGenerateType.Lua)
            {
                CheckLuaViewNullNode(collector, errorList);
                return;
            }

            CheckCSharpViewNullNode(collector, errorList);
        }

        private static void CheckCSharpViewNullNode(YouBindCollector collector, List<CheckerErrorBase> errorList)
        {
            if (collector == null || errorList == null) return;

            var generatedComponent = collector.gameObject.GetComponent(collector.targetClassName);
            if (generatedComponent == null)
            {
                errorList.Add(new CheckerErrorBase
                {
                    refObject = collector.gameObject,
                    message = $"未找到生成组件 [{collector.targetClassName}]，需要更新代码并赋值引用。",
                    autoFixFunc = () => AutoFixCSharpViewNullNode(collector)
                });
                return;
            }

            var viewField = generatedComponent.GetType().GetField("view",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (viewField == null)
                return;

            var viewObject = viewField.GetValue(generatedComponent);
            if (viewObject == null)
            {
                errorList.Add(new CheckerErrorBase
                {
                    refObject = generatedComponent,
                    message = $"生成组件 [{generatedComponent.GetType().Name}] 的 view 为空，需要更新代码并赋值引用。",
                    autoFixFunc = () => AutoFixCSharpViewNullNode(collector)
                });
                return;
            }

            var nullFieldNameList = new List<string>();
            var viewFieldArray = viewObject.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (var i = 0; i < viewFieldArray.Length; i++)
            {
                var field = viewFieldArray[i];
                if (!typeof(Object).IsAssignableFrom(field.FieldType))
                    continue;

                var value = field.GetValue(viewObject) as Object;
                if (value == null)
                    nullFieldNameList.Add(field.Name);
            }

            if (nullFieldNameList.Count <= 0)
                return;

            var previewCount = Math.Min(5, nullFieldNameList.Count);
            var previewNameList = nullFieldNameList.GetRange(0, previewCount);
            var preview = string.Join(", ", previewNameList);
            if (nullFieldNameList.Count > previewCount)
                preview += "...";

            errorList.Add(new CheckerErrorBase
            {
                refObject = generatedComponent,
                message = $"生成组件 view 中有空节点（{nullFieldNameList.Count} 个）：{preview}。需要更新代码并赋值引用。",
                autoFixFunc = () => AutoFixCSharpViewNullNode(collector)
            });
        }

        private static bool CheckLuaViewListMismatch(
            YouBindCollector collector,
            Component luaViewComponent,
            IList<Object> viewList,
            List<CheckerErrorBase> errorList)
        {
            if (collector == null || luaViewComponent == null || errorList == null) return false;

            var expectedCount = 0;
            for (var i = 0; i < collector.bindInfoList.Count; i++)
            {
                var bindInfo = collector.bindInfoList[i];
                if (bindInfo == null || !bindInfo.genCode || bindInfo.bindObject == null)
                    continue;
                expectedCount++;
            }

            var actualCount = viewList == null ? 0 : viewList.Count;
            if (expectedCount == actualCount)
                return false;

            errorList.Add(new CheckerErrorBase
            {
                refObject = luaViewComponent,
                message = $"CommonLuaView.viewList 数量不匹配，期望 {expectedCount}，实际 {actualCount}。需要重新生成并初始化引用。",
                autoFixFunc = () => AutoFixLuaViewNullNode(collector)
            });
            return true;
        }

        private static void CheckLuaViewNullNode(YouBindCollector collector, List<CheckerErrorBase> errorList)
        {
            var luaViewType = FindTypeByName("CommonLuaView");
            if (luaViewType == null)
            {
                errorList.Add(new CheckerErrorBase
                {
                    refObject = collector.gameObject,
                    message = "未找到 CommonLuaView 类型，请确认脚本已编译。",
                    autoFixFunc = () => AutoFixLuaViewNullNode(collector)
                });
                return;
            }

            var luaView = collector.gameObject.GetComponent(luaViewType);
            if (luaView == null)
            {
                errorList.Add(new CheckerErrorBase
                {
                    refObject = collector.gameObject,
                    message = "未找到 CommonLuaView 组件，需要补充组件并初始化引用。",
                    autoFixFunc = () => AutoFixLuaViewNullNode(collector)
                });
                return;
            }

            var luaViewClassName = GetStringFieldOrProperty(luaView, "className");
            if (string.IsNullOrEmpty(luaViewClassName) || luaViewClassName != collector.targetClassName)
            {
                errorList.Add(new CheckerErrorBase
                {
                    refObject = luaView,
                    message = $"CommonLuaView.className 异常，期望 [{collector.targetClassName}]，实际 [{luaViewClassName}]。",
                    autoFixFunc = () => AutoFixLuaViewNullNode(collector)
                });
            }

            var viewList = GetObjectListFieldOrProperty(luaView, "viewList");
            if (viewList == null)
            {
                errorList.Add(new CheckerErrorBase
                {
                    refObject = luaView,
                    message = "CommonLuaView.viewList 为空，需要重新初始化引用。",
                    autoFixFunc = () => AutoFixLuaViewNullNode(collector)
                });
                return;
            }

            var nullIndexList = new List<int>();
            for (var i = 0; i < viewList.Count; i++)
            {
                if (viewList[i] == null)
                    nullIndexList.Add(i);
            }

            if (nullIndexList.Count > 0)
            {
                var previewCount = Math.Min(5, nullIndexList.Count);
                var preview = string.Join(", ", nullIndexList.GetRange(0, previewCount));
                if (nullIndexList.Count > previewCount)
                    preview += "...";

                errorList.Add(new CheckerErrorBase
                {
                    refObject = luaView,
                    message = $"CommonLuaView.viewList 中有空引用（{nullIndexList.Count} 个），索引：{preview}。",
                    autoFixFunc = () => AutoFixLuaViewNullNode(collector)
                });
            }

            CheckLuaViewListMismatch(collector, luaView, viewList, errorList);
        }

        private static bool AutoFixCSharpViewNullNode(YouBindCollector collector)
        {
            if (collector == null) return false;

            // 先更新代码
            YouBindCodeGenerater.Instance.DoGenerate(collector);

            // 再确保组件存在并执行引用赋值
            var controller = YouBindCollectorController.Instance;
            controller.SetRootBindBase(collector);

            var generatedComponent = collector.gameObject.GetComponent(collector.targetClassName);
            if (generatedComponent == null)
                generatedComponent = controller.AddBindview(collector);
            if (generatedComponent == null)
            {
                Debug.LogWarning($"快速修复失败：未找到并且无法添加生成组件 [{collector.targetClassName}]。", collector);
                return false;
            }

            var initializeViewMethod = generatedComponent.GetType().GetMethod("InitializeView",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (initializeViewMethod == null)
            {
                Debug.LogWarning($"快速修复失败：生成组件 [{generatedComponent.GetType().Name}] 不存在 InitializeView 方法。", generatedComponent);
                return false;
            }

            try
            {
                initializeViewMethod.Invoke(generatedComponent, null);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"快速修复失败：调用 InitializeView 异常。{e.Message}", generatedComponent);
                return false;
            }

            EditorUtility.SetDirty(generatedComponent);
            return true;
        }

        private static bool AutoFixLuaViewNullNode(YouBindCollector collector)
        {
            if (collector == null) return false;

            // 先更新代码
            YouBindCodeGenerater.Instance.DoGenerate(collector);

            var luaViewType = FindTypeByName("CommonLuaView");
            if (luaViewType == null)
            {
                Debug.LogWarning("快速修复失败：未找到 CommonLuaView 类型。", collector);
                return false;
            }

            var luaView = collector.gameObject.GetComponent(luaViewType);
            if (luaView == null)
                luaView = Undo.AddComponent(collector.gameObject, luaViewType);
            if (luaView == null)
            {
                Debug.LogWarning("快速修复失败：无法添加 CommonLuaView 组件。", collector);
                return false;
            }

            try
            {
                var initializeViewMethod = luaViewType.GetMethod("InitializeView",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (initializeViewMethod == null)
                {
                    Debug.LogWarning("快速修复失败：CommonLuaView 不存在 InitializeView 方法。", luaView);
                    return false;
                }
                initializeViewMethod.Invoke(luaView, null);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"快速修复失败：调用 CommonLuaView.InitializeView 异常。{e.Message}", luaView);
                return false;
            }

            EditorUtility.SetDirty(luaView);
            return true;
        }

        private static Type FindTypeByName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            var assemblyArray = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblyArray.Length; i++)
            {
                var assembly = assemblyArray[i];
                var type = assembly.GetType(typeName);
                if (type != null)
                    return type;
            }

            return null;
        }

        private static string GetStringFieldOrProperty(Component component, string memberName)
        {
            if (component == null || string.IsNullOrEmpty(memberName))
                return string.Empty;

            var type = component.GetType();
            var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
                return field.GetValue(component) as string ?? string.Empty;

            var property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
                return property.GetValue(component, null) as string ?? string.Empty;

            return string.Empty;
        }

        private static IList<Object> GetObjectListFieldOrProperty(Component component, string memberName)
        {
            if (component == null || string.IsNullOrEmpty(memberName))
                return null;

            var type = component.GetType();
            var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
                return field.GetValue(component) as IList<Object>;

            var property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
                return property.GetValue(component, null) as IList<Object>;

            return null;
        }
    }
}
