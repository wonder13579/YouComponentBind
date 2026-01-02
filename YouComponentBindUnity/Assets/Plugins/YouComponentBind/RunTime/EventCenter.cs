using System;
using System.Collections.Generic;
using Object = System.Object;

// 把界面的事件注册也放这里
// 先留接口
namespace YouComponentBind
{
    public static class YouEventCenter
    {
        public static YouEventContent eventContent = new YouEventContent();
        public static Dictionary<string, List<Action>> eventDict = new Dictionary<string, List<Action>>();

        public static void SendMessage(string eventName)
        {
            List<Action> eventList;
            if (!eventDict.TryGetValue(eventName, out eventList)) return;

            eventContent.Clear();
        }

        public static void AddListener(string eventName, Action eventFunction)
        {
            List<Action> eventList;
            if (!eventDict.TryGetValue(eventName, out eventList)) eventDict[eventName] = eventList = new List<Action>();

            eventList.Add(eventFunction);
        }

        public static void RemoveListener(string eventName, Action eventFunction)
        {
            List<Action> eventList;
            if (!eventDict.TryGetValue(eventName, out eventList)) return;

            eventList.Remove(eventFunction);
            if (eventList.Count <= 0) eventDict.Remove(eventName);
        }
    }

    public class YouEventContent
    {
        public float floatValue;
        public int intValue;

        public Object objectValue;

        // 可自行拓展
        public void Clear()
        {
            intValue = 0;
            floatValue = 0;
        }
    }

    public static class YouEventType
    {
        public static string OnPlayerLogin = "OnPlayerLogin";
    }
}