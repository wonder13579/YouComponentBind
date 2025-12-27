using System.Collections.Generic;
using System;
using UnityEngine.Events;
using UnityEngine.UI;

public static class YouComponentBindDefine
{
    public static Dictionary<Type, List<BindEventInfo>> fieldInfos = new()
    {
        {
            typeof(Button), new()
            {
                new() { EventName = "OnClick" },
            }
        },
        {
            typeof(Toggle), new()
            {
                new() { EventName = "OnValueChanged" },
            }
        },
        {
            typeof(InputField), new()
            {
                new() { EventName = "OnValueChange" },
                new() { EventName = "OnEndEdit" },
            }
        },
    };
}