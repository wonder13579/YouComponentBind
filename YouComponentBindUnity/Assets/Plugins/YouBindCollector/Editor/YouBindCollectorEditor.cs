using UnityEditor;
using UnityEngine;

namespace YouBindCollector
{
    [CustomEditor(typeof(YouBindCollector), true)]
    public class YouBindCollectorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var targetComponent = target as YouBindCollector;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("一键更新代码", GUILayout.Height(30)))
            {
                YouBindCollectorController.Instance.UpdateCode(targetComponent);
            }
            if (GUILayout.Button("打开工具箱", GUILayout.Height(30)))
                YouBindCollectorWindow.OpenWindow(targetComponent);

            GUILayout.EndHorizontal();
            DrawDefaultInspector();
        }
    }
}
