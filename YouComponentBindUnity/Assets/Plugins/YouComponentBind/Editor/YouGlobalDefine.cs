using UnityEngine;

namespace YouComponentBind
{
    public static class YouGlobalDefine
    {
        public static string YouComponentBind_RefreshAfterGenCode = "YouComponentBind_RefreshAfterGenCode";

        // .g.cs文件输出目录。就是不能手动修改的
        public static string GetCSharpGenCodeFilePath(string className)
        {
            return $"{Application.dataPath}/Plugins/YouComponentBind/Gen/{className}.g.cs";
        }
        // .cs文件输出目录。就是可以手动修改的。
        public static string GetCSharpCustomCodeFilePath(string className)
        {
            return $"{Application.dataPath}/Plugins/YouComponentBind/Gen/{className}.cs";
        }

        public static string GetFieldName(BindObjectInfo bindInfo, YouObjectBindConfig bindConfig = null, Transform objectTF = null)
        {
            if (bindInfo == null)
                return "";
            if (bindConfig == null)
                bindConfig = YouBindConfigManager.Instance.GetBindConfig(bindInfo.bindType);
            if (objectTF == null)
                objectTF = YouBindCollectorController.GetObjectTransform(bindInfo.bindObject);
            return bindConfig?.prefix + objectTF?.name;
        }
    }
}
