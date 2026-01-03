using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace YouComponentBind
{
    // 这里用来做代码生成 C#代码生成
    public class YouComponentBindCodeGenerater
    {
        public static YouComponentBindCodeGenerater Instance { get; private set; } = new YouComponentBindCodeGenerater();
        public YouBindBase rootBindBase { get; set; }
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
public partial class @ClassName@ : YouBindBase
{
    private @ClassName@View view;

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

public partial class @ClassName@View
{
@FieldDefinition@
}

public interface I@ClassName@EventFunction
{
@EventDefinition@
}
";

        public void DoGenerate(YouBindBase rootBindBase, string outputPath)
        {
            var resultBuilder = new StringBuilder(templeteCSharpGenCode);
            this.rootBindBase = rootBindBase;
            Init();
            if (rootBindBase?.bindInfoList == null)
            {
                Debug.LogError("生成失败 rootBindBase为空");
                return;
            }
            for (int i = 0; i < rootBindBase.bindInfoList.Count; i++)
            {
                var bindInfo = rootBindBase.bindInfoList[i];
                if (bindInfo == null)
                {
                    Debug.LogWarning("请注意有bindInfo为空");
                    return;
                }

                GenerateObject(bindInfo);
                GenerateEvent(bindInfo);
            }
            var nameSpaceContent = string.Concat(nameSpaceList.Select(nameSpace => $"using {nameSpace};\n"));
            var className = rootBindBase.transform.name;
            resultBuilder.Replace("@ClassName@", className);
            resultBuilder.Replace("@NameSpace@", nameSpaceContent);
            resultBuilder.Replace("@FieldAssignment@", fieldAssignmentBuilder.ToString());
            resultBuilder.Replace("@AddListener@", addListenerBuilder.ToString());
            resultBuilder.Replace("@RemoveListener@", removeListenerBuilder.ToString());
            resultBuilder.Replace("@FieldDefinition@", fieldDefinitionBuilder.ToString());
            resultBuilder.Replace("@EventDefinition@", eventDefinitionBuilder.ToString());
            SaveToFile(outputPath, resultBuilder.ToString(), true);
            Debug.Log("代码生成完毕，保存路径为" + outputPath); 
        }

        public void SaveToFile(string filePath, string content, bool overwrite = false)
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

        public void Init()
        {
            nameSpaceList.Clear();
            nameSpaceList.Add("YouComponentBind");
            fieldAssignmentBuilder.Clear();
            addListenerBuilder.Clear();
            removeListenerBuilder.Clear();
            fieldDefinitionBuilder.Clear();
            eventDefinitionBuilder.Clear();
        }

        public void GenerateObject(ComponentBindInfo bindInfo)
        {
            if (bindInfo?.bindType == null) return;
            if (!bindInfo.genCode) return;
            if (bindInfo.fieldName == null) return;
            var nameSpace = bindInfo.bindType.Namespace;
            if (!nameSpaceList.Contains(nameSpace))
                nameSpaceList.Add(nameSpace);
            // 我们需要对GameObject和Transform进行单独处理
            if (bindInfo.bindType == typeof(GameObject))
            {
                fieldAssignmentBuilder.AppendLine(
                    //        view.btnExitButton = transform.Find("a/b/c").GetComponent<Button>();
                    $"        view.{bindInfo.fieldName} = transform.Find(\"{bindInfo.relativePath}\").gameObject;");
            }
            else if (bindInfo.bindType == typeof(RectTransform))
            {
                fieldAssignmentBuilder.AppendLine(
                    //        view.btnExitButton = transform.Find("a/b/c").GetComponent<Button>();
                    $"        view.{bindInfo.fieldName} = transform.Find(\"{bindInfo.relativePath}\") as RectTransform;");
            }
            else if (bindInfo.bindType == typeof(Transform))
            {
                fieldAssignmentBuilder.AppendLine(
                    //        view.btnExitButton = transform.Find("a/b/c").GetComponent<Button>();
                    $"        view.{bindInfo.fieldName} = transform.Find(\"{bindInfo.relativePath}\");");
            }
            else
            {
                fieldAssignmentBuilder.AppendLine(
                    //        view.btnExitButton = transform.Find("a/b/c").GetComponent<Button>();
                    $"        view.{bindInfo.fieldName} = transform.Find(\"{bindInfo.relativePath}\").GetComponent<{bindInfo.bindType.Name}>();");
            }
            fieldDefinitionBuilder.AppendLine(
                //    public Button btnExitButton;
                $"    public {bindInfo.bindType.Name} {bindInfo.fieldName};");
        }

        public void GenerateEvent(ComponentBindInfo bindInfo)
        {
            if (bindInfo?.eventInfoList == null || bindInfo.eventInfoList.Count <= 0)
                return;
            var fieldName = bindInfo.fieldName;
            for (int i = 0; i < bindInfo.eventInfoList.Count; i++)
            {
                var eventInfo = bindInfo.eventInfoList[i];
                var eventConfig = YouBindConfigManager.Instance.GetEventConfig(bindInfo.bindType, eventInfo.eventName);
                if (eventConfig == null) continue;

                var eventFuncName = eventConfig.eventFuncFormat.Replace("@XXX@", fieldName);
                addListenerBuilder.AppendLine(
                    //        view.btnExitButton.onClick.AddListener(OnClickExitButton);
                    $"        view.{fieldName}.{eventConfig.eventName}.AddListener({eventFuncName});");
                removeListenerBuilder.AppendLine(
                    //        view.btnExitButton.onClick.RemoveListener(OnClickExitButton);
                    $"        view.{fieldName}.{eventConfig.eventName}.RemoveListener({eventFuncName});");
                eventDefinitionBuilder.AppendLine(
                    //    void OnExitButtonClick();
                    $"    void {eventFuncName}();");
            }
        }
    }
}
