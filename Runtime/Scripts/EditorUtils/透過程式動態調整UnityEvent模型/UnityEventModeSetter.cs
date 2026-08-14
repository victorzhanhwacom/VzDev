#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public static class UnityEventModeSetter
{
    // target: 擁有這個 UnityEvent 欄位的 MonoBehaviour/ScriptableObject
    // fieldName: UnityEvent 欄位的名稱，例如 "onClick"
    public static void SetTriggerMode(Object target, string fieldName, UnityEventCallState state)
    {
        var so = new SerializedObject(target);
        var eventProp = so.FindProperty(fieldName);
        if (eventProp == null)
        {
            Debug.LogWarning($"找不到欄位: {fieldName}");
            return;
        }

        var calls = eventProp.FindPropertyRelative("m_PersistentCalls.m_Calls");
        for (int i = 0; i < calls.arraySize; i++)
        {
            var call = calls.GetArrayElementAtIndex(i);
            var callState = call.FindPropertyRelative("m_CallState");
            callState.enumValueIndex = (int)state;
        }

        so.ApplyModifiedProperties();
    }
}
#endif