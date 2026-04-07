using System.Text;
using UnityEngine;

namespace YouBindCollector
{
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

            var eventDefinitionWithName = eventConfig.eventDefinition.Replace("@EventName@", fieldName);
            eventDefinitionBuilder.AppendLine($"    public {eventDefinitionWithName}");
            eventDefinitionBuilder.AppendLine("    {");
            eventDefinitionBuilder.AppendLine("        throw new System.NotImplementedException();");
            eventDefinitionBuilder.AppendLine("    }");
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
}
