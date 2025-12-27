using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class YouComponentBindWindow : EditorWindow
{
    public YouBindBase rootBindBase;

    public static void OpenWindow()
    {
        var window = GetWindow<YouComponentBindWindow>("YouComponentBindWindow");
    }

    private void OnSelectionChange()
    {
        rootBindBase = GetFirstComponentInParent<YouBindBase>(Selection.activeTransform);
        Repaint();
    }

    // 为了兼容性考虑我们不用ODIN和uiToolKit
    private void OnGUI()
    {
        EditorGUILayout.ObjectField("目标根物体", rootBindBase?.gameObject, typeof(GameObject), false);
        if (!rootBindBase)
            return;
        foreach (var bindInfo in rootBindBase.bindInfoList)
        {
            ShowBindInfo(bindInfo);
        }
    }

    private bool foldoutValue = false;
    public void ShowBindInfo(BindComponentInfo info)
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(foldoutValue?"-":"+", GUILayout.Width(20)))
        {
            foldoutValue = !foldoutValue;
        }
        
        // 显示引用
        // 显示go或者component，用户不用关心他的类型
        if (info.component != null)
        {
            EditorGUILayout.ObjectField(info.component, typeof(Component), false);
        }
        else if (info.go != null)
        {
            EditorGUILayout.ObjectField(info.go, typeof(GameObject), false);
        }
        else
        {
            // Debug.LogError("引用丢失");
            return;
        }

        // 显示事件列表
        var showEventStr = "";
        if (info.eventInfoList is not null and {Count: > 0})
        {
            showEventStr = string.Concat(
                info.eventInfoList.Select(
                    p => p.EventName + ", "));
        }
        GUILayout.Label(showEventStr);
        GUILayout.EndHorizontal();
        
        if (foldoutValue)
        {
            GUILayout.BeginHorizontal();
            foreach (var eventInfo in info.eventInfoList)
            {
                GUILayout.Toggle(false, eventInfo.EventName);
            }
            GUILayout.EndHorizontal();
        }
    }

    public T GetFirstComponentInParent<T>(Transform target) where T : YouBindBase
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