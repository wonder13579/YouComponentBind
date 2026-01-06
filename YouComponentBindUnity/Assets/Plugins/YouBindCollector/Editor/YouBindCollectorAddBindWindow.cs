using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace YouBindCollector
{
    public class YouBindCollectorAddBindWindow : EditorWindow
    {
        [MenuItem("Tools/添加绑定工具 YouBindCollectorAddBindWindow")]
        public static void OpenWindow()
        {
            var window = GetWindow<YouBindCollectorWindow>("YouComponentBindWindow");
            window.titleContent = new GUIContent("组件绑定工具 YouComponentBind");
        }
    }
}
