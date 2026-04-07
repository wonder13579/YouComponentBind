using System.Text;
using UnityEngine;

namespace YouBindCollector
{
    // 生成.lua.txt文件
    public class LuaCustomCodeFileGenerater : CodeGeneraterBase
    {
        private StringBuilder eventDefinitionBuilder => codeContentBuilder[0];
        protected override int builderCount => 1;
        private string templateLuaCustomCode =
@"-- 此文件由 YouBindCollector 生成一次，后续不会被覆盖，可自由修改业务逻辑。
--- @type @ClassName@
local @ClassName@ = UIRegistry.Get(UIPanelName.@ClassName@PanelName)

if @ClassName@ == nil then
    error(""[@ClassName@] Panel not found in UIRegistry. Please check g file registration."")
end

function @ClassName@:Start()
    print(""[@ClassName@] Start"")
    -- 测试用，先别删
    if self.view ~= nil then
        for key, value in pairs(self.view) do
            print(""[@ClassName@] "" .. key, value)
        end
    end
    -- self.view.Text_Text.text = ""Hello YouBind!""
end

function @ClassName@:OnEnable()
    print(""[@ClassName@] OnEnable"")
end

function @ClassName@:OnDisable()
    print(""[@ClassName@] OnDisable"")
    -- 预留：面板关闭时的清理逻辑
end

@EventDefinition@";

        public override void AppendObject(BindObjectInfo bindInfo)
        {
            // 不需要处理
        }

        public override void AppendEvent(BindObjectInfo bindInfo, BindEventInfo eventInfo)
        {
            if (bindInfo == null || eventInfo == null) return;
            if (!bindInfo.genCode || !eventInfo.genCode) return;

            var eventConfig = YouBindTypeConfigManager.Instance.GetEventConfig(
                bindInfo.bindType, eventInfo.eventName);
            if (eventConfig == null) return;

            var eventFuncName = eventConfig.eventFuncFormat.Replace("@EventName@", bindInfo.fieldName);
            var eventLogMessage = GetLuaEventLogMessage(bindInfo.fieldName, eventConfig.eventName);
            eventDefinitionBuilder.AppendLine($"--- {bindInfo.fieldName}.{eventConfig.eventName}");
            eventDefinitionBuilder.AppendLine($"function {className}:{eventFuncName}(...)");
            eventDefinitionBuilder.AppendLine($"    print(\"[{className}] {EscapeLuaString(eventLogMessage)}\")");
            eventDefinitionBuilder.AppendLine("end");
            eventDefinitionBuilder.AppendLine();
        }

        private static string GetLuaEventLogMessage(string fieldName, string eventName)
        {
            if (eventName == "onClick")
                return $"{fieldName} clicked.";
            if (eventName == "onValueChanged")
                return $"{fieldName} value changed.";
            if (eventName == "onEndEdit")
                return $"{fieldName} end edit.";
            return $"{fieldName} {eventName}.";
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
            var resultBuilder = new StringBuilder(templateLuaCustomCode);
            resultBuilder.Replace("@ClassName@", className);
            resultBuilder.Replace("@EventDefinition@", eventDefinitionBuilder.ToString());

            if (string.IsNullOrEmpty(outputFilePath))
                outputFilePath = YouBindGlobalDefine.GetLuaCustomCodeFilePath(className);
            YouBindCodeGenerater.SaveToFile(outputFilePath, resultBuilder.ToString(), false);
            Debug.Log("代码生成完毕，保存路径为" + outputFilePath);
        }
    }
}
