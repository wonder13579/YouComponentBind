using UnityEngine;
using XLua;

namespace Example.LuaDemo
{
    public class LuaTransformDemo : MonoBehaviour
    {
        [SerializeField] private Transform targetTransform;
        [SerializeField] private string luaResourcePath = "Lua/transform_demo";

        private LuaEnv luaEnv;
        private LuaFunction printTransformFunc;

        private void Start()
        {
            luaEnv = new LuaEnv();

            var luaAsset = LoadLuaAsset();
            if (luaAsset == null)
            {
                Debug.LogError(
                    $"Lua file not found in Resources. tried: {luaResourcePath}, {luaResourcePath}.lua, {TrimLuaSuffix(luaResourcePath)}, {TrimLuaSuffix(luaResourcePath)}.lua",
                    this);
                return;
            }

            luaEnv.DoString(luaAsset.text, luaAsset.name);

            printTransformFunc = luaEnv.Global.Get<LuaFunction>("print_transform_from_cs");
            if (printTransformFunc == null)
            {
                Debug.LogError("Lua function not found: print_transform_from_cs", this);
                return;
            }

            var tf = targetTransform != null ? targetTransform : transform;
            printTransformFunc.Call(tf);
        }

        private TextAsset LoadLuaAsset()
        {
            // For files named like xxx.lua.txt, Unity's resource name is usually xxx.lua.
            var basePath = TrimLuaSuffix(luaResourcePath);
            var tryPathArray = new[]
            {
                luaResourcePath,
                luaResourcePath + ".lua",
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
            printTransformFunc?.Dispose();
            printTransformFunc = null;

            luaEnv?.Dispose();
            luaEnv = null;
        }
    }
}
