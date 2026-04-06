using System;
using System.Collections.Generic;
using UnityEngine;
using XLua;
using Object = UnityEngine.Object;

// For lua-driven views. Avoid generating a C# view class for each screen.
public class CommonLuaView : MonoBehaviour
{
#if UNITY_EDITOR
    public YouBindCollector.YouBindCollector collector;

    public virtual void Reset()
    {
        InitializeView();
    }

    [ContextMenu("Initialize View List"), ExecuteInEditMode]
    public void InitializeView()
    {
        if (collector == null)
            collector = GetComponent<YouBindCollector.YouBindCollector>();
        if (collector == null)
            collector = GetComponentInParent<YouBindCollector.YouBindCollector>();

        viewList.Clear();
        if (collector == null)
        {
            className = string.Empty;
            Debug.LogWarning("CommonLuaView: collector is null, can not initialize view.", this);
            return;
        }

        className = collector.targetClassName;
        var bindInfoList = collector.bindInfoList;
        if (bindInfoList == null)
            return;

        for (var i = 0; i < bindInfoList.Count; i++)
        {
            var bindInfo = bindInfoList[i];
            if (bindInfo == null || !bindInfo.genCode || bindInfo.bindObject == null)
                continue;
            viewList.Add(bindInfo.bindObject);
        }
    }
#endif

    public string className;
    public string luaBasePath = "Lua/Gen";
    public List<Object> viewList = new List<Object>();

    // TODO 项目中应当使用LuaSystem中的lua环境
    // TODO 初始化lua环境。调用FirstLuaWindow.lua和FirstLuaWindow.bind.lua来初始化view。
    private void Start()
    {
        throw new NotImplementedException();
    }
}
