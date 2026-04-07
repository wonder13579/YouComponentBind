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
    private bool luaReady;

    /// <summary>
    /// 初始化Lua环境并加载视图
    /// </summary>
    private void Start()
    {
        // 获取LuaSystem实例
        var luaSystem = LuaSystem.instance;
        if (luaSystem == null)
        {
            Debug.LogError("CommonLuaView: LuaSystem instance is null, can not initialize lua environment.", this);
            return;
        }

        // 加载对应的Lua文件
        if (!string.IsNullOrEmpty(className))
        {
            // 构造Lua文件路径
            string luaGenFilePath = $"{luaBasePath}/{className}.g";
            luaSystem.LoadLuaFile(luaGenFilePath);
            string luaFilePath = $"{luaBasePath}/{className}";
            luaSystem.LoadLuaFile(luaFilePath);

            // 调用初始化函数，传入自身（CommonLuaView），让 Lua 可访问 viewList
            string initFunctionName = $"{className}_Init";
            luaSystem.DoLuaViewFunction(initFunctionName, this);
            luaReady = true;
        }
        else
        {
            Debug.LogError("CommonLuaView: className is empty, can not load lua file.", this);
        }
    }

    private void OnEnable()
    {
        if (!luaReady || string.IsNullOrEmpty(className))
            return;

        CallLuaViewFunctionIfExists($"{className}_OnEnable");
    }

    private void OnDisable()
    {
        if (!luaReady || string.IsNullOrEmpty(className))
            return;

        CallLuaViewFunctionIfExists($"{className}_OnDisable");
    }

    private void CallLuaViewFunctionIfExists(string functionName)
    {
        var luaSystem = LuaSystem.instance;
        if (luaSystem == null)
            return;

        var luaEnv = luaSystem.GetLuaEnv();
        if (luaEnv == null)
            return;

        var luaFunction = luaEnv.Global.Get<LuaFunction>(functionName);
        if (luaFunction == null)
            return;

        try
        {
            luaFunction.Call(this);
        }
        finally
        {
            luaFunction.Dispose();
        }
    }
}
