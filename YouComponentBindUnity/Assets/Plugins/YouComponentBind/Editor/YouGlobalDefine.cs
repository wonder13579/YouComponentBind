using UnityEngine;

namespace YouComponentBind
{
    public static class YouGlobalDefine
    {
        public static string GetCSharpGenCodeFilePath(string className)
        {
            return $"{Application.dataPath}/Plugins/YouComponentBind/Gen/{className}.g.cs";
        }
        public static string GetCSharpCustomCodeFilePath(string className)
        {
            return $"{Application.dataPath}/Plugins/YouComponentBind/Gen/{className}.cs";
        }
    }
}
