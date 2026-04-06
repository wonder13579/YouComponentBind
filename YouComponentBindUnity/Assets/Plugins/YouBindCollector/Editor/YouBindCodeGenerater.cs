using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
                var fieldName = bindInfo.fieldName;
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

    public class CodeGeneraterBase
    {
        public string className;
        public string outputFilePath;
        protected List<string> nameSpaceList = new List<string>();
        protected StringBuilder[] codeContentBuilder = null;
        protected virtual int builderCount => 0;

        public virtual void Clear()
        {
            if (codeContentBuilder == null)
                codeContentBuilder = new StringBuilder[builderCount];
            for (int i = 0; i < builderCount; i++)
            {
                if (codeContentBuilder[i] == null)
                    codeContentBuilder[i] = new StringBuilder();
            }
            className = "";
            outputFilePath = "";
            nameSpaceList.Clear();
            nameSpaceList.Add("UnityEngine");
            foreach (var builder in codeContentBuilder)
                builder.Clear();
        }
        public virtual void AppendObject(BindObjectInfo bindInfo) { }
        public virtual void AppendEvent(BindObjectInfo bindInfo, BindEventInfo eventInfo) { }
        public virtual void ExportCodeFile()
        {
            // 清理最后的空行
            for (int i = 0; i < builderCount; i++)
            {
                var builder = codeContentBuilder[i];
                if (builder == null)
                    continue;
                while (true)
                {
                    if (builder.Length < 1)
                        break;
                    var lastChar = builder[builder.Length - 1];
                    if (lastChar != '\n' && lastChar != '\r')
                        break;
                    builder.Remove(builder.Length - 1, 1);
                }
            }
        }

    }

    // 生成.g.cs文件
    public class CSharpGenCodeFileGenerater : CodeGeneraterBase
    {
        // 字段赋值代码块
        private StringBuilder fieldAssignmentBuilder => codeContentBuilder[0];
        // 消息注册代码块
        private StringBuilder addListenerBuilder => codeContentBuilder[1];
        // 消息注销代码块
        private StringBuilder removeListenerBuilder => codeContentBuilder[2];
        // 字段定义代码块
        private StringBuilder fieldDefinitionBuilder => codeContentBuilder[3];
        // 事件定义代码块
        private StringBuilder eventDefinitionBuilder => codeContentBuilder[4];
        protected override int builderCount => 5;
        // 代码基础模板
        private string templeteCSharpGenCode =
    @"@NameSpace@
// 此文件由YouBindCollector生成，请勿修改。可参考YouBindCollectorWindow
public partial class @ClassName@
{
    [SerializeField]
    private @ClassName@View view = new @ClassName@View();

    public virtual void Reset()
    {
        InitializeView();
    }

    [ContextMenu(""为view上引用的字段赋值""), ExecuteInEditMode]
    public void InitializeView()
    {
@FieldAssignment@
    }

    public virtual void OnEnable()
    {
@AddListener@
    }

    public virtual void OnDisable()
    {
@RemoveListener@
    }
}

[System.Serializable]
public partial class @ClassName@View
{
@FieldDefinition@
}

public interface I@ClassName@EventFunction
{
@EventDefinition@
}
";

        public override void AppendObject(BindObjectInfo bindInfo)
        {
            if (bindInfo?.bindType == null) return;
            if (!bindInfo.genCode) return;
            if (bindInfo.fieldName == null) return;
            var nameSpace = bindInfo.bindType.Namespace;
            if (!nameSpaceList.Contains(nameSpace))
                nameSpaceList.Add(nameSpace);
            // 不使用保存的relativePath，重新获取更稳定。
            var objectTF = YouBindUtils.GetObjectTransform(bindInfo.bindObject);
            var root = YouBindCodeGenerater.Instance.rootBindBase?.transform;
            var relativePath = YouBindUtils.GetRelativePath(objectTF, root);
            var hasRelativePath = !string.IsNullOrEmpty(relativePath);
            var bindTypeName = GetTypeNameForCode(bindInfo.bindType);
            // 我们需要对GameObject和Transform进行单独处理
            if (bindInfo.bindType == typeof(GameObject))
            {
                fieldAssignmentBuilder.AppendLine(
                    hasRelativePath
                        //        view.btnExitButton = transform.Find("a/b/c")?.gameObject;
                        ? $"        view.{bindInfo.fieldName} = transform.Find(\"{relativePath}\")?.gameObject;"
                        //        view.btnExitButton = gameObject;
                        : $"        view.{bindInfo.fieldName} = gameObject;");
            }
            else if (bindInfo.bindType == typeof(RectTransform))
            {
                fieldAssignmentBuilder.AppendLine(
                    hasRelativePath
                        //        view.btnExitButton = transform.Find("a/b/c") as RectTransform;
                        ? $"        view.{bindInfo.fieldName} = transform.Find(\"{relativePath}\") as RectTransform;"
                        //        view.btnExitButton = transform as RectTransform;
                        : $"        view.{bindInfo.fieldName} = transform as RectTransform;");
            }
            else if (bindInfo.bindType == typeof(Transform))
            {
                fieldAssignmentBuilder.AppendLine(
                    hasRelativePath
                        //        view.btnExitButton = transform.Find("a/b/c");
                        ? $"        view.{bindInfo.fieldName} = transform.Find(\"{relativePath}\");"
                        //        view.btnExitButton = transform;
                        : $"        view.{bindInfo.fieldName} = transform;");
            }
            else
            {
                fieldAssignmentBuilder.AppendLine(
                    hasRelativePath
                        //        view.btnExitButton = transform.Find("a/b/c")?.GetComponent<Button>();
                        ? $"        view.{bindInfo.fieldName} = transform.Find(\"{relativePath}\")?.GetComponent<{bindTypeName}>();"
                        //        view.btnExitButton = GetComponent<Button>();
                        : $"        view.{bindInfo.fieldName} = GetComponent<{bindTypeName}>();");
            }
            fieldDefinitionBuilder.AppendLine(
                //    public Button btnExitButton;
                $"    public {bindTypeName} {bindInfo.fieldName};");
        }

        private static string GetTypeNameForCode(System.Type type)
        {
            if (type == null) return "global::System.Object";

            if (!type.IsGenericType)
            {
                var fullName = (type.FullName ?? type.Name).Replace("+", ".");
                return $"global::{fullName}";
            }

            var genericTypeName = type.GetGenericTypeDefinition().FullName ?? type.Name;
            var splitIndex = genericTypeName.IndexOf('`');
            if (splitIndex >= 0)
                genericTypeName = genericTypeName.Substring(0, splitIndex);
            genericTypeName = genericTypeName.Replace("+", ".");
            var genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetTypeNameForCode));
            return $"global::{genericTypeName}<{genericArgs}>";
        }

        public override void AppendEvent(BindObjectInfo bindInfo, BindEventInfo eventInfo)
        {
            if (bindInfo == null || eventInfo == null) return;
            if (!bindInfo.genCode || !eventInfo.genCode) return;
            var fieldName = bindInfo.fieldName;
            var eventConfig = YouBindTypeConfigManager.Instance.GetEventConfig(
                bindInfo.bindType, eventInfo.eventName);
            if (eventConfig == null) return;

            var eventFuncName = eventConfig.eventFuncFormat.Replace("@EventName@", fieldName);
            addListenerBuilder.AppendLine(
                //        view.btnExitButton.onClick.AddListener(OnClickExitButton);
                $"        view.{fieldName}.{eventConfig.eventName}.AddListener({eventFuncName});");
            removeListenerBuilder.AppendLine(
                //        view.btnExitButton.onClick.RemoveListener(OnClickExitButton);
                $"        view.{fieldName}.{eventConfig.eventName}.RemoveListener({eventFuncName});");
            var eventDefinitionStr =
                //    void OnButtonButtonClick();
                $"    {eventConfig.eventDefinition.Replace("@EventName@", fieldName)};";
            eventDefinitionBuilder.AppendLine(eventDefinitionStr);
        }

        public override void ExportCodeFile()
        {
            base.ExportCodeFile();
            var resultBuilder = new StringBuilder(templeteCSharpGenCode);
            var nameSpaceContent = string.Concat(nameSpaceList.Select(nameSpace => $"using {nameSpace};\r\n"));
            resultBuilder.Replace("@ClassName@", className);
            resultBuilder.Replace("@NameSpace@", nameSpaceContent);
            resultBuilder.Replace("@FieldAssignment@", fieldAssignmentBuilder.ToString());
            resultBuilder.Replace("@AddListener@", addListenerBuilder.ToString());
            resultBuilder.Replace("@RemoveListener@", removeListenerBuilder.ToString());
            resultBuilder.Replace("@FieldDefinition@", fieldDefinitionBuilder.ToString());
            resultBuilder.Replace("@EventDefinition@", eventDefinitionBuilder.ToString());

            if (string.IsNullOrEmpty(outputFilePath))
                outputFilePath = YouBindGlobalDefine.GetCSharpGenCodeFilePath(className);
            YouBindCodeGenerater.SaveToFile(outputFilePath, resultBuilder.ToString(), true);
            Debug.Log("代码生成完毕，保存路径为" + outputFilePath);
        }
    }

    // 生成.cs文件
    public class CSharpCustomCodeFileGenerater : CodeGeneraterBase
    {
        // 事件定义代码块
        private StringBuilder eventDefinitionBuilder => codeContentBuilder[0];
        protected override int builderCount => 1;
        // 代码基础模板
        private string templeteCSharpGenCode =
    @"using UnityEngine;

// 此文件由YouBindCollector生成，但是不会覆盖，可将您的逻辑放在这里。
// 新增事件后，可用IDE补全IFirstWindowEventFunction接口，添加新事件。
public partial class @ClassName@ : MonoBehaviour, I@ClassName@EventFunction
{
@EventDefinition@
}
";

        public override void AppendObject(BindObjectInfo bindInfo)
        {
            // 不需要处理
        }

        public override void AppendEvent(BindObjectInfo bindInfo, BindEventInfo eventInfo)
        {
            if (bindInfo == null || eventInfo == null) return;
            var fieldName = bindInfo.fieldName;
            var eventConfig = YouBindTypeConfigManager.Instance.GetEventConfig(
                bindInfo.bindType, eventInfo.eventName);
            if (eventConfig == null) return;
            /*    public void OnInputInputFieldEndEdit(string value)
            */
            var eventDefinitionWithName = eventConfig.eventDefinition.Replace("@EventName@", fieldName);
            eventDefinitionBuilder.AppendLine(
                //    public void OnInputInputFieldEndEdit(string value)
                $"    public {eventDefinitionWithName}");
            eventDefinitionBuilder.AppendLine(
                //    {
                $"    {{");
            eventDefinitionBuilder.AppendLine(
                //        throw new System.NotImplementedException();
                $"        throw new System.NotImplementedException();");
            eventDefinitionBuilder.AppendLine(
                //    }
                $"    }}");
            eventDefinitionBuilder.AppendLine();
        }

        public override void ExportCodeFile()
        {
            base.ExportCodeFile();
            var resultBuilder = new StringBuilder(templeteCSharpGenCode);
            resultBuilder.Replace("@ClassName@", className);
            resultBuilder.Replace("@EventDefinition@", eventDefinitionBuilder.ToString());
            if (string.IsNullOrEmpty(outputFilePath))
                outputFilePath = YouBindGlobalDefine.GetCSharpCustomCodeFilePath(className);
            YouBindCodeGenerater.SaveToFile(outputFilePath, resultBuilder.ToString(), false);
            Debug.Log("代码生成完毕，保存路径为" + outputFilePath);
        }
    }

    // 生成.bind.lua.txt文件
    public class LuaGenCodeFileGenerater : CodeGeneraterBase
    {
        private StringBuilder fieldAssignmentBuilder => codeContentBuilder[0];
        protected override int builderCount => 1;
        private int appendIndex = 0;

        private string templateLuaGenCode =
@"-- Auto-generated by YouBindCollector. Do not modify.
function @ClassName@_InitializeView(root)
    if root == nil then
        print(""[@ClassName@.bind] root is nil"")
        return nil
    end

    local view = {}
@FieldAssignment@

    print(""[@ClassName@.bind] InitializeView result:"")
    for key, value in pairs(view) do
        print(""[@ClassName@.bind] "" .. key, value)
    end
    return view
end
";

        public override void Clear()
        {
            base.Clear();
            appendIndex = 0;
        }

        public override void AppendObject(BindObjectInfo bindInfo)
        {
            if (bindInfo?.bindType == null) return;
            if (!bindInfo.genCode) return;
            if (string.IsNullOrEmpty(bindInfo.fieldName)) return;

            var objectTF = YouBindUtils.GetObjectTransform(bindInfo.bindObject);
            var root = YouBindCodeGenerater.Instance.rootBindBase?.transform;
            var relativePath = YouBindUtils.GetRelativePath(objectTF, root);
            var hasRelativePath = !string.IsNullOrEmpty(relativePath);
            var escapedFieldName = EscapeLuaString(bindInfo.fieldName);
            var nodeVarName = $"node_{appendIndex}";
            appendIndex++;

            if (hasRelativePath)
                fieldAssignmentBuilder.AppendLine($"    local {nodeVarName} = root:Find(\"{EscapeLuaString(relativePath)}\")");
            else
                fieldAssignmentBuilder.AppendLine($"    local {nodeVarName} = root");

            if (bindInfo.bindType == typeof(GameObject))
            {
                fieldAssignmentBuilder.AppendLine($"    view[\"{escapedFieldName}\"] = {nodeVarName} and {nodeVarName}.gameObject or nil");
            }
            else if (bindInfo.bindType == typeof(Transform))
            {
                fieldAssignmentBuilder.AppendLine($"    view[\"{escapedFieldName}\"] = {nodeVarName}");
            }
            else if (bindInfo.bindType == typeof(RectTransform))
            {
                if (hasRelativePath)
                {
                    fieldAssignmentBuilder.AppendLine(
                        $"    view[\"{escapedFieldName}\"] = {nodeVarName} and {nodeVarName}:GetComponent(typeof(CS.UnityEngine.RectTransform)) or nil");
                }
                else
                {
                    fieldAssignmentBuilder.AppendLine(
                        $"    view[\"{escapedFieldName}\"] = root:GetComponent(typeof(CS.UnityEngine.RectTransform))");
                }
            }
            else
            {
                var luaTypeName = GetLuaTypeName(bindInfo.bindType);
                fieldAssignmentBuilder.AppendLine(
                    $"    view[\"{escapedFieldName}\"] = {nodeVarName} and {nodeVarName}:GetComponent(typeof({luaTypeName})) or nil");
            }
            fieldAssignmentBuilder.AppendLine();
        }

        private static string GetLuaTypeName(System.Type type)
        {
            if (type == null) return "CS.System.Object";
            if (type.IsGenericType) return "CS.System.Object";
            var fullName = (type.FullName ?? type.Name).Replace("+", ".");
            return $"CS.{fullName}";
        }

        private static string EscapeLuaString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        public override void ExportCodeFile()
        {
            base.ExportCodeFile();
            var resultBuilder = new StringBuilder(templateLuaGenCode);
            resultBuilder.Replace("@ClassName@", className);
            resultBuilder.Replace("@FieldAssignment@", fieldAssignmentBuilder.ToString());

            if (string.IsNullOrEmpty(outputFilePath))
                outputFilePath = YouBindGlobalDefine.GetLuaGenCodeFilePath(className);
            YouBindCodeGenerater.SaveToFile(outputFilePath, resultBuilder.ToString(), true);
            Debug.Log("代码生成完毕，保存路径为" + outputFilePath);
        }
    }

    // 生成.lua.txt文件
    public class LuaCustomCodeFileGenerater : CodeGeneraterBase
    {
        protected override int builderCount => 1;
        private string templateLuaCustomCode =
@"-- Generated once by YouBindCollector. This file will not be overwritten.
function @ClassName@_Init(root)
    local view = @ClassName@_InitializeView(root)
    print(""[@ClassName@] Init done."")
    if view ~= nil then
        for key, value in pairs(view) do
            print(""[@ClassName@] "" .. key, value)
        end
    end
    return view
end
";

        public override void AppendObject(BindObjectInfo bindInfo)
        {
            // 不需要处理
        }

        public override void AppendEvent(BindObjectInfo bindInfo, BindEventInfo eventInfo)
        {
            // 暂不处理事件
        }

        public override void ExportCodeFile()
        {
            base.ExportCodeFile();
            var resultBuilder = new StringBuilder(templateLuaCustomCode);
            resultBuilder.Replace("@ClassName@", className);

            if (string.IsNullOrEmpty(outputFilePath))
                outputFilePath = YouBindGlobalDefine.GetLuaCustomCodeFilePath(className);
            YouBindCodeGenerater.SaveToFile(outputFilePath, resultBuilder.ToString(), false);
            Debug.Log("代码生成完毕，保存路径为" + outputFilePath);
        }
    }
}
