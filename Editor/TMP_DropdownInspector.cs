#if UNITY_EDITOR
using UnityEditor;
using TMPro;
using TMPro.EditorUtilities;

/// <summary>
/// 解決在Editor中修改TMP_Dropdown的value時，onValueChanged事件不會被觸發的問題。
/// </summary>
[CustomEditor(typeof(TMP_Dropdown))]
public class TMP_DropdownInspector : DropdownEditor
{
    private int _previousValue;

    public override void OnInspectorGUI()
    {
        TMP_Dropdown dropdown = (TMP_Dropdown)target;
        _previousValue = dropdown.value;

        base.OnInspectorGUI(); // 完整保留原本的 Inspector UI

        if (dropdown.value != _previousValue)
        {
            dropdown.onValueChanged.Invoke(dropdown.value);
        }
    }
}
#endif