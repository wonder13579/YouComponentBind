using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class YouBindBase : MonoBehaviour
{
#if UNITY_EDITOR
    public List<BindComponentInfo> bindInfoList = new();
#endif
}

#if UNITY_EDITOR
[System.Serializable]
public class BindComponentInfo
{
    public string relativePath;
    public Component component;
    public GameObject go;
    public List<BindEventInfo> eventInfoList = new();
}

[System.Serializable]
public class BindEventInfo
{
    public BindComponentInfo targetInfo;
    public string EventName; // onClick
    public Component component;
    public GameObject go;
}
#endif