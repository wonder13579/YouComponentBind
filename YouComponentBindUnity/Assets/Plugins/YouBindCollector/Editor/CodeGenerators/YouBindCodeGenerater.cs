using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace YouBindCollector
{
    // 这里用来做代码生成 C#代码生成
    public class YouBindCodeGenerater
    {
        public static YouBindCodeGenerater Instance { get; private set; } = new YouBindCodeGenerater();
        private CSharpGenCodeFileGenerater CSharpGenCodeGenerater = new CSharpGenCodeFileGenerater();
        private CSharpCustomCodeFileGenerater CSharpCustomCodeFileGenerater = new CSharpCustomCodeFileGenerater();
        private LuaGenCodeFileGenerater LuaGenCodeGenerater = new LuaGenCodeFileGenerater();
        private LuaCustomCodeFileGenerater LuaCustomCodeFileGenerater = new LuaCustomCodeFileGenerater();
        private List<CodeGeneraterBase> codeGeneraterList = new List<CodeGeneraterBase>();
        public YouBindCollector rootBindBase { get; set; }

        public void DoGenerate(YouBindCollector rootBindBase)
        {
            this.rootBindBase = rootBindBase;

            if (rootBindBase?.bindInfoList == null)
            {
                Debug.LogError("生成失败 rootBindBase为空");
                return;
            }
            codeGeneraterList.Clear();
            if (rootBindBase.codeGenerateType == YouBindCollector.CodeGenerateType.CSharp)
            {
                codeGeneraterList.Add(CSharpGenCodeGenerater);
                var customFilePath = YouBindGlobalDefine.GetCSharpCustomCodeFilePath(rootBindBase.targetClassName);
                if (!File.Exists(customFilePath))
                    codeGeneraterList.Add(CSharpCustomCodeFileGenerater);
            }
            else
            {
                codeGeneraterList.Add(LuaGenCodeGenerater);
                var luaCustomFilePath = YouBindGlobalDefine.GetLuaCustomCodeFilePath(rootBindBase.targetClassName);
                if (!File.Exists(luaCustomFilePath))
                    codeGeneraterList.Add(LuaCustomCodeFileGenerater);
            }

            codeGeneraterList.ForEach(p => p.Clear());
            codeGeneraterList.ForEach(p => p.className = rootBindBase.targetClassName);

            for (int i = 0; i < rootBindBase.bindInfoList.Count; i++)
            {
                var bindInfo = rootBindBase.bindInfoList[i];
                if (bindInfo == null)
                {
                    Debug.LogWarning("请注意有bindInfo为空");
                    continue;
                }

                codeGeneraterList.ForEach(p => p.AppendObject(bindInfo));
                if (bindInfo?.eventInfoList == null || bindInfo.eventInfoList.Count <= 0)
                    continue;
                for (int j = 0; j < bindInfo.eventInfoList.Count; j++)
                {
                    var eventInfo = bindInfo.eventInfoList[j];
                    codeGeneraterList.ForEach(p => p.AppendEvent(bindInfo, eventInfo));
                }
            }
            codeGeneraterList.ForEach(p => p.ExportCodeFile());
            codeGeneraterList.ForEach(p => p.Clear());

            var refreshAfterGenCode = EditorPrefs.GetBool(YouBindGlobalDefine.YouComponentBind_RefreshAfterGenCode, true);
            if (refreshAfterGenCode)
                AssetDatabase.Refresh();
        }

        public static void SaveToFile(string filePath, string content, bool overwrite = false)
        {
            // 获取文件所在目录
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (overwrite || !File.Exists(filePath))
            {
                File.WriteAllText(filePath, content);
            }
        }
    }
}
