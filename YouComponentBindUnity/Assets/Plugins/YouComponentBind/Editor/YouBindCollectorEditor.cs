using UnityEditor;
using UnityEngine;

namespace YouComponentBind
{
    [CustomEditor(typeof(YouBindCollector), true)]
    public class YouBindCollectorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var targetComponent = target as YouBindCollector;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("更新代码", GUILayout.Height(30)))
            {
                YouBindCollectorController.Instance.UpdateCode(targetComponent);
            }
            if (GUILayout.Button("打开工具箱", GUILayout.Height(20), GUILayout.Width(70)))
                YouBindCollectorWindow.OpenWindow();

            GUILayout.EndHorizontal();
            DrawDefaultInspector();
        }
    }
}