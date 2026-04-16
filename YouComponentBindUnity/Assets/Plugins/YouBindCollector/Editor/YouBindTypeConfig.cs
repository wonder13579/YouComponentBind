using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace YouBindCollector
{
    public class YouBindTypeConfigManager
    {
        // 缓存
        private readonly Dictionary<Type, YouBindTypeConfig>
            bindConfigDict = new Dictionary<Type, YouBindTypeConfig>();

        private readonly List<YouBindTypeConfig> bindConfigList = new List<YouBindTypeConfig>();
        public static YouBindTypeConfigManager Instance { get; private set; } = new YouBindTypeConfigManager();

        public YouBindTypeConfig GetBindConfig(Type componentType)
        {
            if (componentType == null)
                return null;
            if (bindConfigList.Count <= 0)
                Init();
            // 缓存
            YouBindTypeConfig ans = null;
            if (bindConfigDict.TryGetValue(componentType, out ans))
                return ans;
            // 支持继承
            for (var i = 0; i < bindConfigList.Count; i++)
                if (bindConfigList[i].bindType.IsAssignableFrom(componentType))
                    ans = bindConfigList[i];
            // 无配置
            if (ans == null)
            {
                ans = YouBindTypeConfig.Default(componentType);
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

        public void AddConfig(Type type, bool autoBind = true,
            YouEventBindConfig[] eventArray = null)
        {
            bindConfigList.Add(new YouBindTypeConfig
            {
                bindType = type,
                autoBind = autoBind,
                eventArray = eventArray
            });
        }

        private void Init()
        {
            AddConfig(typeof(Transform), false);
            AddConfig(typeof(RectTransform), false);
            AddConfig(typeof(GameObject), false);
            AddConfig(typeof(Text));
            AddConfig(typeof(Image), false);
            AddConfig(typeof(RawImage));
            AddConfig(typeof(Button), eventArray: new[]
            {
                new YouEventBindConfig("onClick", "On@EventName@Click", "void On@EventName@Click()")
            });
            AddConfig(typeof(Toggle), eventArray: new[]
            {
                new YouEventBindConfig("onValueChanged", "On@EventName@ValueChanged", "void On@EventName@ValueChanged(bool value)")
            });
            AddConfig(typeof(InputField), eventArray: new[]
            {
                new YouEventBindConfig("onValueChanged", "On@EventName@ValueChanged", "void On@EventName@ValueChanged(string value)"),
                new YouEventBindConfig("onEndEdit", "On@EventName@EndEdit", "void On@EventName@EndEdit(string value)")
            });
            bindConfigList.ForEach(p => { bindConfigDict[p.bindType] = p; });
        }
    }

    // 用于定义组件如何生成。可对每种组件单独写功能。
    // 配置不用ScriptableObject，代码文件对查引用更友好
    public class YouBindTypeConfig : IYouCodeBindGenerate
    {
        public bool autoBind; // 扫描时默认加入组件列表
        public Type bindType;
        public YouEventBindConfig[] eventArray; // 支持生成的事件列表

        // 给没有配置的生成一个默认配置
        public static YouBindTypeConfig Default(Type bindType)
        {
            return new YouBindTypeConfig()
            {
                autoBind = false,
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
