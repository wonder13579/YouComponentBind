using UnityEditor;
using UnityEngine;

namespace YouComponentBind
{
    [CustomEditor(typeof(YouBindBase), true)]
    public class MyCustomComponentEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // 获取目标组件实例
            var targetComponent = target as YouBindBase;

            // 添加自定义按钮
            if (GUILayout.Button("Custom Button")) YouComponentBindWindow.OpenWindow();

            // 绘制默认的Inspector字段
            DrawDefaultInspector();
        }
    }
}