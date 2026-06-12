#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VzDev.ToolUtils.ThemeUtils.Editor
{
    [CustomEditor(typeof(ThemeData))]
    public class ThemeDataEditor : UnityEditor.Editor
    {
        // 格子固定寬度，不隨 Inspector 拉伸
        private const float CELL_W    = 80f;
        private const float CELL_SIZE = 80f;   // 正方形：寬 = 高
        private const float HEX_H     = 13f;
        private const float ENUM_H    = 18f;
        private const float BTN_H     = 18f;
        private const float CELL_PAD  = 3f;
        private const float CELL_GAP  = 6f;    // 格子間距

        // Font 欄位 label 固定寬度，讓右側 value 對齊
        private const float FONT_LABEL_W = 80f;

        private SerializedProperty _tokensProp;
        private SerializedProperty _fontsProp;

        private bool _showColors = true;
        private bool _showFonts  = true;

        private int _colorDeleteIndex = -1;
        private int _fontDeleteIndex  = -1;

        private void OnEnable()
        {
            _tokensProp = serializedObject.FindProperty("tokens");
            _fontsProp  = serializedObject.FindProperty("fonts");
        }

        // ColorPicker 的 mouseover 預覽不會寫回 SerializedProperty
        // 需要每幀重繪才能讓 hex 碼即時更新
        public override bool RequiresConstantRepaint() => true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // ── Color Tokens ──────────────────────────────────────────
            _showColors = EditorGUILayout.Foldout(_showColors, "Color Tokens", true, EditorStyles.foldoutHeader);
            if (_showColors)
            {
                EditorGUILayout.Space(4);
                DrawArrayControls(_tokensProp, typeof(ColorToken));
                EditorGUILayout.Space(4);

                if (_tokensProp.arraySize == 0)
                    EditorGUILayout.HelpBox("No color tokens defined. Press + to add.", MessageType.Info);
                else
                    DrawColorGrid();

                EditorGUILayout.Space(8);
            }

            // ── Font Tokens ───────────────────────────────────────────
            _showFonts = EditorGUILayout.Foldout(_showFonts, "Font Tokens", true, EditorStyles.foldoutHeader);
            if (_showFonts)
            {
                EditorGUILayout.Space(4);
                DrawArrayControls(_fontsProp, typeof(FontToken));
                EditorGUILayout.Space(4);

                for (int i = 0; i < _fontsProp.arraySize; i++)
                    DrawFontEntry(i);

                EditorGUILayout.Space(4);
            }

            serializedObject.ApplyModifiedProperties();

            if (_colorDeleteIndex >= 0)
            {
                _tokensProp.DeleteArrayElementAtIndex(_colorDeleteIndex);
                _colorDeleteIndex = -1;
                serializedObject.ApplyModifiedProperties();
            }
            if (_fontDeleteIndex >= 0)
            {
                _fontsProp.DeleteArrayElementAtIndex(_fontDeleteIndex);
                _fontDeleteIndex = -1;
                serializedObject.ApplyModifiedProperties();
            }
        }

        // ── Color grid — 固定格子寬度，自動換行 ───────────────────────
        private void DrawColorGrid()
        {
            int   count    = _tokensProp.arraySize;
            float cellStep = CELL_W + CELL_GAP;
            float gridW    = EditorGUIUtility.currentViewWidth - 32f;
            // 每行能放幾個
            int   cols     = Mathf.Max(1, Mathf.FloorToInt((gridW + CELL_GAP) / cellStep));
            int   rows     = Mathf.CeilToInt((float)count / cols);
            float cellH    = HEX_H + CELL_SIZE + ENUM_H + BTN_H + CELL_PAD * 4;

            Rect gridRect = GUILayoutUtility.GetRect(gridW, rows * (cellH + CELL_GAP));

            for (int i = 0; i < count; i++)
            {
                int   col = i % cols;
                int   row = i / cols;
                float x   = gridRect.x + col * cellStep;
                float y   = gridRect.y + row * (cellH + CELL_GAP);

                SerializedProperty entryProp = _tokensProp.GetArrayElementAtIndex(i);
                SerializedProperty colorProp = entryProp.FindPropertyRelative("color");
                SerializedProperty tokenProp = entryProp.FindPropertyRelative("token");

                float curY = y + CELL_PAD;

                // 1. Swatch（正方形）— 用 ColorField 取得即時預覽色（含 hover）
                Rect swatchRect = new Rect(x, curY + HEX_H + CELL_PAD, CELL_W, CELL_SIZE);
                EditorGUI.BeginChangeCheck();
                Color displayColor = EditorGUI.ColorField(
                    swatchRect, GUIContent.none,
                    colorProp.colorValue,
                    showEyedropper: true, showAlpha: true, hdr: false);
                if (EditorGUI.EndChangeCheck())
                    colorProp.colorValue = displayColor;

                // 2. Hex code — 從 ColorField 回傳的即時色讀取（hover 時也會更新）
                string hex = ColorUtility.ToHtmlStringRGB(displayColor);
                var hexStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    fontSize  = 9,
                    alignment = TextAnchor.UpperCenter,
                    normal    = { textColor = new Color(0.6f, 0.9f, 0.6f) }
                };
                GUI.Label(new Rect(x, curY, CELL_W, HEX_H), $"#{hex}", hexStyle);
                curY += HEX_H + CELL_PAD + CELL_SIZE + CELL_PAD;

                // 3. Enum dropdown
                EditorGUI.PropertyField(
                    new Rect(x, curY, CELL_W, ENUM_H),
                    tokenProp, GUIContent.none);
                curY += ENUM_H + CELL_PAD;

                // 4. Remove button
                var redStyle = new GUIStyle(EditorStyles.miniButton)
                    { normal = { textColor = new Color(1f, 0.4f, 0.4f) } };
                if (GUI.Button(new Rect(x, curY, CELL_W, BTN_H), "✕", redStyle))
                    _colorDeleteIndex = i;
            }
        }

        // ── Font entry — label 固定寬，value 對齊 ─────────────────────
        private void DrawFontEntry(int index)
        {
            SerializedProperty entryProp     = _fontsProp.GetArrayElementAtIndex(index);
            SerializedProperty fontTokenProp = entryProp.FindPropertyRelative("token");

            string label = fontTokenProp != null
                ? fontTokenProp.enumNames[fontTokenProp.enumValueIndex]
                : $"Element {index}";

            // Foldout header + remove button
            EditorGUILayout.BeginHorizontal();
            entryProp.isExpanded = EditorGUILayout.Foldout(entryProp.isExpanded, label, true);
            var redStyle = new GUIStyle(EditorStyles.miniButton)
                { normal = { textColor = new Color(1f, 0.4f, 0.4f) } };
            if (GUILayout.Button("✕", redStyle, GUILayout.Width(24)))
                _fontDeleteIndex = index;
            EditorGUILayout.EndHorizontal();

            if (!entryProp.isExpanded) return;

            // 手動繪製每個子欄位，固定 label 寬度讓 value 對齊
            float savedLabelW = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = FONT_LABEL_W;

            EditorGUI.indentLevel++;
            SerializedProperty child = entryProp.Copy();
            SerializedProperty end   = entryProp.GetEndProperty();
            child.NextVisible(true);
            while (!SerializedProperty.EqualContents(child, end))
            {
                EditorGUILayout.PropertyField(child, true);
                if (!child.NextVisible(false)) break;
            }
            EditorGUI.indentLevel--;

            EditorGUIUtility.labelWidth = savedLabelW;
        }

        // ── Array controls ─────────────────────────────────────────────
        private void DrawArrayControls(SerializedProperty arrayProp, Type enumType)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Count: {arrayProp.arraySize}", EditorStyles.miniLabel, GUILayout.Width(70));

            if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(24)))
            {
                arrayProp.arraySize++;
                var newEntry  = arrayProp.GetArrayElementAtIndex(arrayProp.arraySize - 1);
                var tokenProp = newEntry.FindPropertyRelative("token");
                if (tokenProp != null)
                    tokenProp.enumValueIndex = NextUnusedEnumIndex(arrayProp, enumType);
            }

            if (GUILayout.Button("−", EditorStyles.miniButton, GUILayout.Width(24)))
            {
                if (arrayProp.arraySize > 0) arrayProp.arraySize--;
            }

            EditorGUI.BeginChangeCheck();
            int newSize = EditorGUILayout.IntField(arrayProp.arraySize, GUILayout.Width(40));
            if (EditorGUI.EndChangeCheck())
                arrayProp.arraySize = Mathf.Max(0, newSize);

            EditorGUILayout.EndHorizontal();
        }

        private int NextUnusedEnumIndex(SerializedProperty arrayProp, Type enumType)
        {
            int enumCount = Enum.GetValues(enumType).Length;
            var used = new HashSet<int>();
            for (int i = 0; i < arrayProp.arraySize - 1; i++)
            {
                var tp = arrayProp.GetArrayElementAtIndex(i).FindPropertyRelative("token");
                if (tp != null) used.Add(tp.enumValueIndex);
            }
            for (int i = 0; i < enumCount; i++)
                if (!used.Contains(i)) return i;
            return 0;
        }
    }
}
#endif
