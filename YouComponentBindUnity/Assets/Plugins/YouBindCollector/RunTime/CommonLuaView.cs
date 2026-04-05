// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using XLua;
//
// public class CommonLuaView : MonoBehaviour {
//     public TextAsset viewLuaCode;
//     public TextAsset initializeViewLuaCode;
//     public List<Object> viewList = new List<Object>();
//
//     public virtual void Reset()
//     {
//         InitializeView();
//     }
//
//     [ContextMenu("为view上引用的字段赋值"), ExecuteInEditMode]
//     public void InitializeView()
//     {
//         if (initializeViewLuaCode == null)
//             return;
//         LuaEnv luaEnv = new LuaEnv();
//         luaEnv.DoString(initializeViewLuaCode.text);
//         var func = luaEnv.Global.Get<LuaFunction>("ExampleWindow_InitializeView");
//         func.Call(transform);
//
//         luaEnv.Dispose();
//     }
//
//     public virtual void OnEnable()
//     {
//     }
//
//     public virtual void OnDisable()
//     {
//     }
// }
