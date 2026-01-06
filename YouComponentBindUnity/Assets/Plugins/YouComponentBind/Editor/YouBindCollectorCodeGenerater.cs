using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace YouComponentBind
{
    // 这里用来做代码生成 C#代码生成
    public class YouBindCollectorCodeGenerater
    {
        public static YouBindCollectorCodeGenerater Instance { get; private set; } = new YouBindCollectorCodeGenerater();
        private CSharpGenCodeFileGenerater CSharpGenCodeGenerater = new CSharpGenCodeFileGenerater();
        private CSharpCustomCodeFileGenerater CSharpCustomCodeFileGenerater = new CSharpCustomCodeFileGenerater();
        public YouBindCollector rootBindBase { get; set; }

        public void DoGenerate(YouBindCollector rootBindBase)
        {
            this.rootBindBase = rootBindBase;

            if (rootBindBase?.bindInfoList == null)
            {
                Debug.LogError("生成失败 rootBindBase为空");
                return;
            }
            CSharpGenCodeGenerater.Clear();
            CSharpGenCodeGenerater.className = rootBindBase.transform.name;

            CSharpCustomCodeFileGenerater.Clear();
            CSharpCustomCodeFileGenerater.className = rootBindBase.transform.name;
            for (int i = 0; i < rootBindBase.bindInfoList.Count; i++)
            {
                var bindInfo = rootBindBase.bindInfoList[i];
                if (bindInfo == null)
                {
                    Debug.LogWarning("请注意有bindInfo为空");
                    continue;
                }

                CSharpGenCodeGenerater.AppendObject(bindInfo);
                CSharpCustomCodeFileGenerater.AppendObject(bindInfo);
                if (bindInfo?.eventInfoList == null || bindInfo.eventInfoList.Count <= 0)
                    continue;
                var fieldName = bindInfo.fieldName;
                for (int j = 0; j < bindInfo.eventInfoList.Count; j++)
                {
                    var eventInfo = bindInfo.eventInfoList[j];
                    CSharpGenCodeGenerater.AppendEvent(bindInfo, eventInfo);
                    CSharpCustomCodeFileGenerater.AppendEvent(bindInfo, eventInfo);
                }
            }
            CSharpGenCodeGenerater.ExportCodeFile();
            CSharpCustomCodeFileGenerater.ExportCodeFile();

            CSharpGenCodeGenerater.Clear();
            CSharpCustomCodeFileGenerater.Clear();
            var refreshAfterGenCode = EditorPrefs.GetBool(YouGlobalDefine.YouComponentBind_RefreshAfterGenCode, true);
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

    // 生成.g.cs文件
    public class CSharpGenCodeFileGenerater
    {
        public string className;
        public string outputFilePath;
        // 命名空间收集
        private List<string> nameSpaceList = new List<string>();
        // 字段赋值代码块
        private StringBuilder fieldAssignmentBuilder = new StringBuilder();
        // 消息注册代码块
        private StringBuilder addListenerBuilder = new StringBuilder();
        // 消息注销代码块
        private StringBuilder removeListenerBuilder = new StringBuilder();
        // 字段定义代码块
        private StringBuilder fieldDefinitionBuilder = new StringBuilder();
        // 事件定义代码块
        private StringBuilder eventDefinitionBuilder = new StringBuilder();
        // 代码基础模板
        private string templeteCSharpGenCode =
    @"@NameSpace@

// 此文件由YouComponentBind生成，请勿修改。可参考YouComponentBindWindow
public partial class @ClassName@
{
    [UnityEngine.SerializeField]
    private @ClassName@View view = new @ClassName@View();

    public virtual void Reset()
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


        public void Clear()
        {
            className = "";
            nameSpaceList.Clear();
            nameSpaceList.Add("YouComponentBind");
            fieldAssignmentBuilder.Clear();
            addListenerBuilder.Clear();
            removeListenerBuilder.Clear();
            fieldDefinitionBuilder.Clear();
            eventDefinitionBuilder.Clear();
        }

        public void AppendObject(BindObjectInfo bindInfo)
        {
            if (bindInfo?.bindType == null) return;
            if (!bindInfo.genCode) return;
            if (bindInfo.fieldName == null) return;
            var nameSpace = bindInfo.bindType.Namespace;
            if (!nameSpaceList.Contains(nameSpace))
                nameSpaceList.Add(nameSpace);
            // 不使用保存的relativePath，重新获取更稳定。
            var objectTF = YouBindCollectorController.GetObjectTransform(bindInfo.bindObject);
            var root = YouBindCollectorCodeGenerater.Instance.rootBindBase?.transform;
            var relativePath = YouBindCollectorController.GetRelativePath(objectTF, root);
            // 我们需要对GameObject和Transform进行单独处理
            if (bindInfo.bindType == typeof(GameObject))
            {
                fieldAssignmentBuilder.AppendLine(
                    //        view.btnExitButton = transform.Find("a/b/c")?.gameObject;
                    $"        view.{bindInfo.fieldName} = transform.Find(\"{relativePath}\")?.gameObject;");
            }
            else if (bindInfo.bindType == typeof(RectTransform))
            {
                fieldAssignmentBuilder.AppendLine(
                    //        view.btnExitButton = transform.Find("a/b/c") as RectTransform;
                    $"        view.{bindInfo.fieldName} = transform.Find(\"{relativePath}\") as RectTransform;");
            }
            else if (bindInfo.bindType == typeof(Transform))
            {
                fieldAssignmentBuilder.AppendLine(
                    //        view.btnExitButton = transform.Find("a/b/c");
                    $"        view.{bindInfo.fieldName} = transform.Find(\"{relativePath}\");");
            }
            else
            {
                fieldAssignmentBuilder.AppendLine(
                    //        view.btnExitButton = transform.Find("a/b/c")?.GetComponent<Button>();
                    $"        view.{bindInfo.fieldName} = transform.Find(\"{relativePath}\")?.GetComponent<{bindInfo.bindType.Name}>();");
            }
            fieldDefinitionBuilder.AppendLine(
                //    public Button btnExitButton;
                $"    public {bindInfo.bindType.Name} {bindInfo.fieldName};");
        }

        public void AppendEvent(BindObjectInfo bindInfo, BindEventInfo eventInfo)
        {
            if (bindInfo == null || eventInfo == null) return;
            var fieldName = bindInfo.fieldName;
            var eventConfig = YouBindConfigManager.Instance.GetEventConfig(
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

        public void ExportCodeFile()
        {
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
                outputFilePath = YouGlobalDefine.GetCSharpGenCodeFilePath(className);
            YouBindCollectorCodeGenerater.SaveToFile(outputFilePath, resultBuilder.ToString(), true);
            Debug.Log("代码生成完毕，保存路径为" + outputFilePath);
        }
    }

    // 生成.cs文件
    public class CSharpCustomCodeFileGenerater
    {
        public string className;
        public string outputFilePath;
        // 事件定义代码块
        private StringBuilder eventDefinitionBuilder = new StringBuilder();
        // 代码基础模板
        private string templeteCSharpGenCode =
    @"using UnityEngine;


// 此文件由YouComponentBind生成，但是不会覆盖，请将您的逻辑放在这里。
// 新增事件后，可用IDE补全IFirstWindowEventFunction接口，添加新事件。
public partial class @ClassName@ : MonoBehaviour, I@ClassName@EventFunction
{
@EventDefinition@
}
";
        public void Clear()
        {
            eventDefinitionBuilder.Clear();
        }

        public void AppendObject(BindObjectInfo bindInfo)
        {
            // 不需要处理
        }

        public void AppendEvent(BindObjectInfo bindInfo, BindEventInfo eventInfo)
        {
            if (bindInfo == null || eventInfo == null) return;
            var fieldName = bindInfo.fieldName;
            var eventConfig = YouBindConfigManager.Instance.GetEventConfig(
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

        public void ExportCodeFile()
        {
            var resultBuilder = new StringBuilder(templeteCSharpGenCode);
            resultBuilder.Replace("@ClassName@", className);
            resultBuilder.Replace("@EventDefinition@", eventDefinitionBuilder.ToString());
            if (string.IsNullOrEmpty(outputFilePath))
                outputFilePath = YouGlobalDefine.GetCSharpCustomCodeFilePath(className);
            YouBindCollectorCodeGenerater.SaveToFile(outputFilePath, resultBuilder.ToString(), false);
            Debug.Log("代码生成完毕，保存路径为" + outputFilePath);
        }
    }
}