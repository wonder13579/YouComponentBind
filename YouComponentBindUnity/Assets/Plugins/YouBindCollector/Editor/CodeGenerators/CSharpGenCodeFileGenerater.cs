using System.Linq;
using System.Text;
using UnityEngine;

namespace YouBindCollector
{
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
                        ? $"        view.{bindInfo.fieldName} = transform.Find(\"{relativePath}\")?.gameObject;"
                        : $"        view.{bindInfo.fieldName} = gameObject;");
            }
            else if (bindInfo.bindType == typeof(RectTransform))
            {
                fieldAssignmentBuilder.AppendLine(
                    hasRelativePath
                        ? $"        view.{bindInfo.fieldName} = transform.Find(\"{relativePath}\") as RectTransform;"
                        : $"        view.{bindInfo.fieldName} = transform as RectTransform;");
            }
            else if (bindInfo.bindType == typeof(Transform))
            {
                fieldAssignmentBuilder.AppendLine(
                    hasRelativePath
                        ? $"        view.{bindInfo.fieldName} = transform.Find(\"{relativePath}\");"
                        : $"        view.{bindInfo.fieldName} = transform;");
            }
            else
            {
                fieldAssignmentBuilder.AppendLine(
                    hasRelativePath
                        ? $"        view.{bindInfo.fieldName} = transform.Find(\"{relativePath}\")?.GetComponent<{bindTypeName}>();"
                        : $"        view.{bindInfo.fieldName} = GetComponent<{bindTypeName}>();");
            }
            fieldDefinitionBuilder.AppendLine($"    public {bindTypeName} {bindInfo.fieldName};");
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
                $"        view.{fieldName}.{eventConfig.eventName}.AddListener({eventFuncName});");
            removeListenerBuilder.AppendLine(
                $"        view.{fieldName}.{eventConfig.eventName}.RemoveListener({eventFuncName});");
            var eventDefinitionStr =
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
}
