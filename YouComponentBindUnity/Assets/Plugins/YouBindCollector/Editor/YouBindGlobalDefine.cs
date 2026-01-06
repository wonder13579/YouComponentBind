using UnityEngine;

namespace YouBindCollector
{
    public static class YouBindGlobalDefine
    {
        public static string YouComponentBind_RefreshAfterGenCode = "YouComponentBind_RefreshAfterGenCode";

        // .g.cs文件输出目录。就是不能手动修改的
        public static string GetCSharpGenCodeFilePath(string className)
        {
            return $"{Application.dataPath}/Example/Scripts/Gen/{className}.g.cs";
        }
        // .cs文件输出目录。就是可以手动修改的。
        public static string GetCSharpCustomCodeFilePath(string className)
        {
            return $"{Application.dataPath}/Example/Scripts/Gen/{className}.cs";
        }

        public static string GetTargetClassName(YouBindCollector collector)
        {
            return collector.transform.name;
        }

        public static string GetFieldName(BindObjectInfo bindInfo, YouBindTypeConfig bindConfig = null, Transform objectTF = null)
        {
            if (bindInfo == null)
                return "";
            if (bindConfig == null)
                bindConfig = YouBindTypeConfigManager.Instance.GetBindConfig(bindInfo.bindType);
            if (objectTF == null)
                objectTF = YouBindCollectorController.GetObjectTransform(bindInfo.bindObject);
            return bindConfig?.prefix + objectTF?.name;
        }
    }
}
