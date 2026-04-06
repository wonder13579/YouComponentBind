using UnityEngine;

namespace YouBindCollector
{
    // 将一些自定义内容集中到这里，方便接入项目时根据需要修改。
    public static class YouBindGlobalDefine
    {
        public static string YouComponentBind_RefreshAfterGenCode = "YouComponentBind_RefreshAfterGenCode";
        public static string YouComponentBind_ShowHierarchyMarkInEditMode = "YouComponentBind_ShowHierarchyMarkInEditMode";
        public static string YouComponentBind_ShowNoGenCodeComponent = "YouComponentBind_ShowNoGenCodeComponent";

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

        // 生成类的命名
        public static string GetTargetClassName(YouBindCollector collector)
        {
            return collector.transform.name;
        }

        // 生成组件的命名
        public static string GetFieldName(BindObjectInfo bindInfo, YouBindTypeConfig bindConfig = null, Transform objectTF = null)
        {
            if (bindInfo == null)
                return "";
            if (bindConfig == null)
                bindConfig = YouBindTypeConfigManager.Instance.GetBindConfig(bindInfo.bindType);
            if (objectTF == null)
                objectTF = YouBindUtils.GetObjectTransform(bindInfo.bindObject);
            return bindConfig?.prefix + objectTF?.name;
        }
    }
}
