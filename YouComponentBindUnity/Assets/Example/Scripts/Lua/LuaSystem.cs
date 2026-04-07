using System;
using UnityEngine;
using XLua;

// 提供测试用Lua环境
public class LuaSystem : MonoBehaviour
{
    public static LuaSystem instance;

    private void Awake()
    {
        instance = this;
        luaEnv = new LuaEnv();
    }

    private LuaEnv luaEnv;

    /// <summary>
    /// 其他地方调用这个来加载lua文件
    /// </summary>
    /// <param name="luaFilePath"></param>
    public void LoadLuaFile(string luaFilePath)
    {
        var luaAsset = LoadLuaAsset(luaFilePath);
        if (luaAsset == null)
        {
            Debug.LogError(
                $"Lua file not found in Resources: {luaFilePath}",
                this);
            return;
        }

        luaEnv.DoString(luaAsset.text, luaAsset.name);
    }

    /// <summary>
    /// 执行lua函数
    /// </summary>
    /// <param name="functionName"></param>
    /// <param name="tf"></param>
    public void DoLuaTransformFunction(string functionName, Transform tf)
    {
        var luaFunction = luaEnv.Global.Get<LuaFunction>(functionName);
        if (luaFunction == null)
        {
            Debug.LogError($"Lua luaFunction not found: {functionName}", this);
            return;
        }

        luaFunction.Call(tf);
    }

    /// <summary>
    /// 获取Lua环境
    /// </summary>
    /// <returns></returns>
    public LuaEnv GetLuaEnv()
    {
        return luaEnv;
    }

    private TextAsset LoadLuaAsset(string luaFilePath)
    {
        // For files named like xxx.lua.txt, Unity's resource name is usually xxx.lua.
        var basePath = TrimLuaSuffix(luaFilePath);
        var tryPathArray = new[]
        {
            luaFilePath,
            luaFilePath + ".lua",
            basePath,
            basePath + ".lua"
        };

        for (var i = 0; i < tryPathArray.Length; i++)
        {
            var path = tryPathArray[i];
            var asset = Resources.Load<TextAsset>(path);
            if (asset != null)
                return asset;
        }

        return null;
    }

    private static string TrimLuaSuffix(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;
        if (path.EndsWith(".lua"))
            return path.Substring(0, path.Length - 4);
        return path;
    }

    private void Update()
    {
        if (luaEnv != null)
            luaEnv.Tick();
    }

    private void OnDestroy()
    {
        luaEnv?.Dispose();
        luaEnv = null;
    }
}
