using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace YouComponentBind
{
    public class YouBindConfigManager
    {
        public static string CSharpGenCodePath = Application.dataPath + "/Plugins/YouComponentBind/Gen";
        // 缓存
        private readonly Dictionary<Type, YouComponentBindConfig>
            bindConfigDict = new Dictionary<Type, YouComponentBindConfig>();

        private readonly List<YouComponentBindConfig> bindConfigList = new List<YouComponentBindConfig>();
        public static YouBindConfigManager Instance { get; private set; } = new YouBindConfigManager();

        public YouComponentBindConfig GetBindConfig(Type componentType)
        {
            if (componentType == null)
                return null;
            if (bindConfigList.Count <= 0)
                Init();
            // 缓存
            YouComponentBindConfig ans = null;
            if (bindConfigDict.TryGetValue(componentType, out ans))
                return ans;
            // 支持继承
            for (var i = 0; i < bindConfigList.Count; i++)
                if (bindConfigList[i].bindType.IsAssignableFrom(componentType))
                    ans = bindConfigList[i];
            // 无配置
            if (ans == null)
            {
                ans = YouComponentBindConfig.Default(componentType);
            }
            bindConfigDict[componentType] = ans;
            return ans;
        }

        public YouEventBindConfig GetEventConfig(Type componentType, string eventName)
        {
            var componentConfig = GetBindConfig(componentType);
            if (componentConfig?.eventArray == null) return null;
            return Array.Find(componentConfig.eventArray, p => p.eventName == eventName);
        }

        public void AddConfig(Type type, string prefix = null, bool autoBind = true,
            YouEventBindConfig[] eventArray = null)
        {
            bindConfigList.Add(new YouComponentBindConfig
            {
                bindType = type,
                prefix = prefix,
                autoBind = autoBind,
                eventArray = eventArray
            });
        }

        private void Init()
        {
            // 前缀功能可能要干掉了
            AddConfig(typeof(Transform), "TF", false);
            AddConfig(typeof(RectTransform), "RTF", false);
            AddConfig(typeof(GameObject), "GO", false);
            AddConfig(typeof(Text), "Text");
            AddConfig(typeof(Image), "Image", false);
            AddConfig(typeof(RawImage), "Raw");
            AddConfig(typeof(Button), "Button", eventArray: new[]
            {
                new YouEventBindConfig("onClick", "On@EventName@Click", "    void On@EventName@Click();")
            });
            AddConfig(typeof(Toggle), "Toggle", eventArray: new[]
            {
                new YouEventBindConfig("onValueChanged", "On@EventName@ValueChanged", "    void On@EventName@ValueChanged(bool value);")
            });
            AddConfig(typeof(InputField), "Input", eventArray: new[]
            {
                new YouEventBindConfig("onValueChanged", "On@EventName@ValueChanged", "    void On@EventName@ValueChanged(string value);"),
                new YouEventBindConfig("onEndEdit", "On@EventName@EndEdit", "    void On@EventName@EndEdit(string value);")
            });
            bindConfigList.ForEach(p => { bindConfigDict[p.bindType] = p; });
        }
    }

    // 用于定义组件如何生成。可对每种组件单独写功能。
    // 配置不用ScriptableObject，代码文件对查引用更友好
    public class YouComponentBindConfig : IYouCodeBindGenerate
    {
        public bool autoBind; // 扫描时默认加入组件列表
        public string prefix;
        public Type bindType;
        public YouEventBindConfig[] eventArray; // 支持生成的事件列表

        // 给没有配置的生成一个默认配置
        public static YouComponentBindConfig Default(Type bindType)
        {
            return new YouComponentBindConfig()
            {
                autoBind = false,
                prefix = bindType.Name,
                bindType = bindType,
                eventArray = null,
            };
        }

        public virtual void DoGenerate()
        {
            throw new NotImplementedException();
        }
    }

    public class YouEventBindConfig : IYouCodeBindGenerate
    {
        public string eventName;
        public string eventFuncFormat;
        public string eventDefinition;
        public bool autoGenerate = true; // 扫描时默认加入此事件

        public YouEventBindConfig(string eventName, string eventFuncFormat, string eventDefinition, bool autoGenerate = true)
        {
            this.eventName = eventName;
            this.eventFuncFormat = eventFuncFormat;
            this.eventDefinition = eventDefinition;
            this.autoGenerate = autoGenerate;
        }

        public virtual void DoGenerate()
        {
            throw new NotImplementedException();
        }
    }

    // 作为生成的接口
    public interface IYouCodeBindGenerate
    {
        void DoGenerate();
    }
}