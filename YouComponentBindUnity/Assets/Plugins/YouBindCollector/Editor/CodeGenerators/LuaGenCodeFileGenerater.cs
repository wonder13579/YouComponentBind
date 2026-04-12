using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace YouBindCollector
{
    // 生成.g.lua.txt文件
    public class LuaGenCodeFileGenerater : CodeGeneraterBase
    {
        private StringBuilder fieldAssignmentBuilder => codeContentBuilder[0];
        private StringBuilder eventFunctionBuilder => codeContentBuilder[1];
        private StringBuilder registerEventBuilder => codeContentBuilder[2];
        private StringBuilder unregisterEventBuilder => codeContentBuilder[3];
        protected override int builderCount => 4;
        private int appendIndex = 0;

        private string templateLuaGenCode =
@"-- 此文件由 YouBindCollector 自动生成，请勿手动修改。

--- @class @ClassName@
local @ClassName@ = {}
UIPanelName = UIPanelName or {}
UIPanelName.@ClassName@ = ""@ClassName@""

UIRegistry.Register(UIPanelName.@ClassName@, @ClassName@)

function Get@ClassName@Panel()
    --- @type @ClassName@
    local panel = UIRegistry.Get(
        UIPanelName.@ClassName@)
    if panel == nil then
        print(""[@ClassName@] Panel not found in UIRegistry."")
    end
    return panel
end

function @ClassName@:InitView(commonView)
    if commonView == nil or commonView.viewList == nil then
        print(""[@ClassName@.bind] commonView is nil"")
        return nil
    end
    local viewList = commonView.viewList

    --- @class @ClassName@View
    local view = {}
@FieldAssignment@
    self.view = view
end

@EventFunction@
function @ClassName@:RegisterEvent()
    self:UnregisterEvent()
@RegisterEvent@
    return self.view
end

function @ClassName@:UnregisterEvent()
@UnregisterEvent@
    return self.view
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
            var displayPath = string.IsNullOrEmpty(relativePath) ? "(root)" : relativePath;
            var escapedFieldName = EscapeLuaIdentifier(bindInfo.fieldName);
            var luaTypeName = GetLuaSimpleTypeName(bindInfo.bindType);

            fieldAssignmentBuilder.AppendLine(
                $"    --- @type {luaTypeName} {EscapeLuaComment(displayPath)} -> {luaTypeName}");
            fieldAssignmentBuilder.AppendLine(
                $"    view.{escapedFieldName} = viewList[{appendIndex}]");
            appendIndex++;
        }

        public override void AppendEvent(BindObjectInfo bindInfo, BindEventInfo eventInfo)
        {
            if (bindInfo == null || eventInfo == null) return;
            if (!bindInfo.genCode || !eventInfo.genCode) return;

            var eventConfig = YouBindTypeConfigManager.Instance.GetEventConfig(
                bindInfo.bindType, eventInfo.eventName);
            if (eventConfig == null) return;

            var fieldName = EscapeLuaIdentifier(bindInfo.fieldName);
            var eventFuncName = eventConfig.eventFuncFormat.Replace("@EventName@", bindInfo.fieldName);
            var wrapperName = $"{className}_{eventFuncName}";
            var paramList = GetLuaParamNameList(
                eventConfig.eventDefinition.Replace("@EventName@", bindInfo.fieldName));

            eventFunctionBuilder.AppendLine(
                $"function {wrapperName}({paramList})");
            eventFunctionBuilder.AppendLine(
                $"    local panel = Get{className}Panel()");
            eventFunctionBuilder.AppendLine(
                $"    if panel ~= nil and panel.{eventFuncName} ~= nil then");
            if (string.IsNullOrEmpty(paramList))
                eventFunctionBuilder.AppendLine($"        panel:{eventFuncName}()");
            else
                eventFunctionBuilder.AppendLine($"        panel:{eventFuncName}({paramList})");
            eventFunctionBuilder.AppendLine(
                $"    end");
            eventFunctionBuilder.AppendLine(
                $"end");
            eventFunctionBuilder.AppendLine();

            registerEventBuilder.AppendLine(
                $"    self.view.{fieldName}.{eventConfig.eventName}:AddListener({wrapperName})");
            unregisterEventBuilder.AppendLine(
                $"    self.view.{fieldName}.{eventConfig.eventName}:RemoveListener({wrapperName})");
        }

        private static string GetLuaSimpleTypeName(System.Type type)
        {
            if (type == null)
                return "Object";
            return type.Name.Replace("`", "_");
        }

        private static string EscapeLuaIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "_";
            return value.Replace(" ", "_").Replace("-", "_");
        }

        private static string EscapeLuaComment(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            return value.Replace("\r", " ").Replace("\n", " ");
        }

        private static string GetLuaParamNameList(string eventDefinitionWithName)
        {
            if (string.IsNullOrEmpty(eventDefinitionWithName))
                return string.Empty;

            var leftBracketIndex = eventDefinitionWithName.IndexOf('(');
            var rightBracketIndex = eventDefinitionWithName.LastIndexOf(')');
            if (leftBracketIndex < 0 || rightBracketIndex <= leftBracketIndex)
                return string.Empty;

            var paramContent = eventDefinitionWithName
                .Substring(leftBracketIndex + 1, rightBracketIndex - leftBracketIndex - 1)
                .Trim();
            if (string.IsNullOrEmpty(paramContent))
                return string.Empty;

            var luaParamList = new List<string>();
            var rawParamArray = paramContent.Split(',');
            for (var i = 0; i < rawParamArray.Length; i++)
            {
                var rawParam = rawParamArray[i].Trim();
                if (string.IsNullOrEmpty(rawParam))
                    continue;

                var tokenArray = rawParam.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (tokenArray.Length <= 0)
                    continue;

                var paramName = tokenArray[tokenArray.Length - 1];
                luaParamList.Add(EscapeLuaIdentifier(paramName));
            }

            return string.Join(", ", luaParamList);
        }

        public override void ExportCodeFile()
        {
            base.ExportCodeFile();
            var resultBuilder = new StringBuilder(templateLuaGenCode);
            resultBuilder.Replace("@ClassName@", className);
            resultBuilder.Replace("@FieldAssignment@", fieldAssignmentBuilder.ToString());
            resultBuilder.Replace("@EventFunction@", eventFunctionBuilder.ToString());
            resultBuilder.Replace("@RegisterEvent@", registerEventBuilder.ToString());
            resultBuilder.Replace("@UnregisterEvent@", unregisterEventBuilder.ToString());

            if (string.IsNullOrEmpty(outputFilePath))
                outputFilePath = YouBindGlobalDefine.GetLuaGenCodeFilePath(className);
            YouBindCodeGenerater.SaveToFile(outputFilePath, resultBuilder.ToString(), true);
            Debug.Log("代码生成完毕，保存路径为" + outputFilePath);
        }
    }
}
