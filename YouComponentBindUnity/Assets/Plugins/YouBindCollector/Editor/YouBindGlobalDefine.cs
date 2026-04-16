using System.Text;
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

        // .g.lua.txt文件输出目录。自动生成，可覆盖。
        public static string GetLuaGenCodeFilePath(string className)
        {
            return $"{Application.dataPath}/Example/Resources/Lua/Gen/{className}.g.lua.txt";
        }

        // .lua.txt文件输出目录。首次生成，不覆盖。
        public static string GetLuaCustomCodeFilePath(string className)
        {
            return $"{Application.dataPath}/Example/Resources/Lua/Gen/{className}.lua.txt";
        }

        // 生成类的命名
        public static string GetTargetClassName(YouBindCollector collector)
        {
            return collector.transform.name;
        }

        // 生成组件的命名
        public static string GetFieldName(BindObjectInfo bindInfo, Transform objectTF = null)
        {
            if (bindInfo == null)
                return "";
            if (objectTF == null)
                objectTF = YouBindUtils.GetObjectTransform(bindInfo.bindObject);
            return BuildFieldName(objectTF?.name);
        }

        public static string BuildFieldName(string rawName)
        {
            return SanitizeIdentifier(rawName, "Object");
        }

        // 替换标识符中的特殊字符
        public static string SanitizeIdentifier(string value, string fallback = "_")
        {
            if (string.IsNullOrEmpty(value))
                return fallback;

            var builder = new StringBuilder(value.Length + 1);
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (char.IsLetterOrDigit(c) || c == '_')
                    builder.Append(c);
                else
                    builder.Append('_');
            }

            if (builder.Length <= 0)
                builder.Append(fallback);
            if (char.IsDigit(builder[0]))
                builder.Insert(0, '_');

            return builder.ToString();
        }
    }
}
