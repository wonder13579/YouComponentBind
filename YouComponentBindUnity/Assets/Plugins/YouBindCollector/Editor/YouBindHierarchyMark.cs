using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using YouBindCollector;

// 已经被绑定的组件，在Hierarchy上显示一个标记
[InitializeOnLoad]
public class YouBindHierarchyMark
{
    static float markWidth = 10f;
    static YouBindHierarchyMark()
    {
        EditorApplication.hierarchyWindowItemOnGUI += HierarchyItemOnGUI;
    }

    private static void HierarchyItemOnGUI(int instanceID, Rect selectionRect)
    {
        GameObject gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (gameObject == null) return;
        var collector = YouBindCollectorController.GetFirstComponentInParent<YouBindCollector.YouBindCollector>(gameObject.transform);
        if(collector == null) return;
        var isJoined = collector.joinedTransformSet.Contains(gameObject.transform);
        if(!isJoined) return;
        selectionRect.xMin = selectionRect.xMax - markWidth;
        EditorGUI.LabelField(selectionRect, new GUIContent("*", "【YouBindCollector】被代码引用，请勿删除"));
    }
}
