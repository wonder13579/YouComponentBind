using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using YouBindCollector;

#if UNITY_2021_2_OR_NEWER
using UnityEditor.SceneManagement;
using PrefabStage = UnityEditor.SceneManagement.PrefabStage;

#else
using UnityEditor.Experimental.SceneManagement;
using PrefabStage = UnityEditor.Experimental.SceneManagement.PrefabStage;
#endif


// 已经被绑定的组件，在Hierarchy上显示一个标记
[InitializeOnLoad]
public class YouBindHierarchyMark
{
    private const float MarkWidth = 10f;
    private static readonly GUIContent MarkContent = new GUIContent("*", "【YouBindCollector】被代码引用，请勿删除");

    // 记录每个collector引用的所有Transform
    private static readonly Dictionary<int, HashSet<Transform>> CollectorTransformCache =
        new Dictionary<int, HashSet<Transform>>();

    // 记录每个Transform引用次数
    private static readonly Dictionary<Transform, int> TransformRefCount =
        new Dictionary<Transform, int>();


    #region 事件注册

    private static bool _isHierarchyHooked;

    static YouBindHierarchyMark()
    {
        PrefabStage.prefabStageOpened += OnPrefabStageOpened;
        PrefabStage.prefabStageClosing += OnPrefabStageClosing;
        SyncHierarchyHookWithPrefabStage();
    }

    private static void OnPrefabStageOpened(PrefabStage prefabStage)
    {
        SyncHierarchyHookWithPrefabStage();
    }

    private static void OnPrefabStageClosing(PrefabStage prefabStage)
    {
        // PrefabStage关闭事件触发时可能正在切换到其它Prefab，延迟一帧再同步状态更稳妥。
        EditorApplication.delayCall += SyncHierarchyHookWithPrefabStage;
    }

    private static void SyncHierarchyHookWithPrefabStage()
    {
        var hasPrefabStage = PrefabStageUtility.GetCurrentPrefabStage() != null;
        var shouldHook = hasPrefabStage && IsHierarchyMarkEnabled();
        SetHierarchyHook(shouldHook);
        if (shouldHook)
            RebuildCache();
        else
            ClearCache();
    }

    private static bool IsHierarchyMarkEnabled()
    {
        return EditorPrefs.GetBool(YouBindGlobalDefine.YouComponentBind_ShowHierarchyMarkInEditMode, true);
    }

    public static void RefreshDisplaySetting()
    {
        SyncHierarchyHookWithPrefabStage();
    }

    private static void SetHierarchyHook(bool shouldHook)
    {
        if (_isHierarchyHooked == shouldHook) return;

        if (shouldHook)
        {
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyItemOnGUI;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }
        else
        {
            EditorApplication.hierarchyWindowItemOnGUI -= HierarchyItemOnGUI;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        _isHierarchyHooked = shouldHook;
        EditorApplication.RepaintHierarchyWindow();
    }


    private static void OnHierarchyChanged()
    {
        if (!_isHierarchyHooked) return;
        RebuildCache();
    }

    private static void OnUndoRedoPerformed()
    {
        if (!_isHierarchyHooked) return;
        RebuildCache();
    }

    public static void NotifyCollectorChanged(YouBindCollector.YouBindCollector collector)
    {
        if (!_isHierarchyHooked) return;
        if (collector == null)
        {
            RebuildCache();
            return;
        }

        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        var stageRoot = prefabStage?.prefabContentsRoot?.transform;
        if (stageRoot == null) return;
        if (!IsUnderRoot(collector.transform, stageRoot)) return;

        UpdateCollectorCache(collector);
        EditorApplication.RepaintHierarchyWindow();
    }

    #endregion

    private static void RebuildCache()
    {
        ClearCache();

        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        var prefabRoot = prefabStage?.prefabContentsRoot;
        if (prefabRoot == null) return;

        var collectorArray = prefabRoot.GetComponentsInChildren<YouBindCollector.YouBindCollector>(true);
        for (var i = 0; i < collectorArray.Length; i++)
        {
            UpdateCollectorCache(collectorArray[i]);
        }

        EditorApplication.RepaintHierarchyWindow();
    }

    private static void UpdateCollectorCache(YouBindCollector.YouBindCollector collector)
    {
        if (collector == null) return;

        var collectorId = collector.GetInstanceID();
        RemoveCollectorCache(collectorId);

        var transformSet = CollectBoundTransformSet(collector);
        if (transformSet.Count <= 0) return;

        CollectorTransformCache[collectorId] = transformSet;
        foreach (var tf in transformSet)
        {
            int count;
            TransformRefCount.TryGetValue(tf, out count);
            TransformRefCount[tf] = count + 1;
        }
    }

    private static HashSet<Transform> CollectBoundTransformSet(YouBindCollector.YouBindCollector collector)
    {
        var ans = new HashSet<Transform>();
        var bindInfoList = collector.bindInfoList;
        if (bindInfoList == null) return ans;

        for (var i = 0; i < bindInfoList.Count; i++)
        {
            var info = bindInfoList[i];
            if (info == null || !info.genCode) continue;
            var tf = info.GetTransform();
            if (tf == null) continue;
            ans.Add(tf);
        }

        return ans;
    }

    private static void RemoveCollectorCache(int collectorId)
    {
        HashSet<Transform> oldSet;
        if (!CollectorTransformCache.TryGetValue(collectorId, out oldSet)) return;

        CollectorTransformCache.Remove(collectorId);
        foreach (var tf in oldSet)
        {
            if (tf == null) continue;

            int count;
            if (!TransformRefCount.TryGetValue(tf, out count)) continue;
            if (count <= 1)
                TransformRefCount.Remove(tf);
            else
                TransformRefCount[tf] = count - 1;
        }
    }

    private static void ClearCache()
    {
        CollectorTransformCache.Clear();
        TransformRefCount.Clear();
    }

    private static bool IsUnderRoot(Transform target, Transform root)
    {
        var cur = target;
        while (cur != null)
        {
            if (cur == root) return true;
            cur = cur.parent;
        }

        return false;
    }

    private static void HierarchyItemOnGUI(int instanceID, Rect selectionRect)
    {
        if (Event.current.type != EventType.Repaint) return;
        if (TransformRefCount.Count <= 0) return;

        var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (gameObject == null) return;
        if (!TransformRefCount.ContainsKey(gameObject.transform)) return;

        selectionRect.xMin = selectionRect.xMax - MarkWidth;
        var oldColor = GUI.color;
        GUI.color = Color.red;
        EditorGUI.LabelField(selectionRect, MarkContent);
        GUI.color = oldColor;
    }
}
