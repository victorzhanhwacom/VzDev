using UnityEngine;
using UnityEditor;
using System.IO;

namespace VzDev.EditorTools
{
    /// <summary>
    /// 將選取物件的 Mesh Pivot 設為模型中心點（或底部/頂部中心）。
    /// 會產生一份新的 Mesh 資源（頂點已偏移），並自動調整 Transform 位置，
    /// 讓物件的視覺位置維持不變。
    /// 使用方式：Unity 選單 VzDev/Tools/Center Pivot
    /// </summary>
    public class CenterPivotWindow : EditorWindow
    {
        private enum PivotMode { BoundsCenter, BottomCenter, TopCenter }

        private PivotMode pivotMode = PivotMode.BoundsCenter;
        private bool saveAsNewAsset = true;
        private string savePath = "Assets/CenteredMeshes";

        [MenuItem("VzDev/Tools/Center Pivot")]
        private static void ShowWindow()
        {
            var window = GetWindow<CenterPivotWindow>("Center Pivot");
            window.minSize = new Vector2(340, 240);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("將選取物件的 Pivot 設為模型中心", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            pivotMode = (PivotMode)EditorGUILayout.EnumPopup("中心點模式", pivotMode);

            saveAsNewAsset = EditorGUILayout.Toggle("另存為新的 Mesh 資源", saveAsNewAsset);

            if (saveAsNewAsset)
            {
                EditorGUILayout.BeginHorizontal();
                savePath = EditorGUILayout.TextField("儲存路徑", savePath);
                if (GUILayout.Button("選擇", GUILayout.Width(50)))
                {
                    string picked = EditorUtility.OpenFolderPanel("選擇儲存路徑", "Assets", "");
                    if (!string.IsNullOrEmpty(picked))
                    {
                        string relative = FileUtil.GetProjectRelativePath(picked);
                        savePath = string.IsNullOrEmpty(relative) ? savePath : relative;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("不另存資源時，會直接在記憶體中建立新的 Mesh 實例並指派給物件（不會寫入專案檔案，重開專案後失效）。建議保持勾選。", MessageType.Warning);
            }

            EditorGUILayout.Space();

            GameObject[] selected = Selection.gameObjects;
            EditorGUILayout.HelpBox($"目前選取了 {selected.Length} 個物件", MessageType.Info);

            using (new EditorGUI.DisabledScope(selected.Length == 0))
            {
                if (GUILayout.Button("執行 Center Pivot", GUILayout.Height(32)))
                {
                    ApplyCenterPivot(selected);
                }
            }
        }

        private void ApplyCenterPivot(GameObject[] selection)
        {
            if (saveAsNewAsset && !Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            int processed = 0;
            int skipped = 0;

            Undo.SetCurrentGroupName("Center Pivot");
            int undoGroup = Undo.GetCurrentGroup();

            foreach (GameObject go in selection)
            {
                MeshFilter mf = go.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null)
                {
                    Debug.LogWarning($"[CenterPivot] 略過 {go.name}：找不到 MeshFilter 或 Mesh。", go);
                    skipped++;
                    continue;
                }

                if (CenterPivotForObject(go, mf, pivotMode, saveAsNewAsset, savePath))
                {
                    processed++;
                }
                else
                {
                    skipped++;
                }
            }

            Undo.CollapseUndoOperations(undoGroup);

            if (saveAsNewAsset)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            EditorUtility.DisplayDialog("Center Pivot", $"完成。\n處理成功：{processed}\n略過：{skipped}", "OK");
        }

        /// <summary>
        /// 核心邏輯：計算 pivot 偏移量、產生偏移後的新 Mesh、調整 Transform 補償位置。
        /// </summary>
        private static bool CenterPivotForObject(GameObject go, MeshFilter mf, PivotMode mode, bool saveAsset, string path)
        {
            Mesh sourceMesh = mf.sharedMesh;
            Bounds bounds = sourceMesh.bounds; // local space bounds

            Vector3 pivotOffset;
            switch (mode)
            {
                case PivotMode.BottomCenter:
                    pivotOffset = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                    break;
                case PivotMode.TopCenter:
                    pivotOffset = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
                    break;
                default: // BoundsCenter
                    pivotOffset = bounds.center;
                    break;
            }

            // 已經在目標中心點，不需處理
            if (pivotOffset.sqrMagnitude < 1e-10f)
            {
                Debug.Log($"[CenterPivot] {go.name} 的 pivot 已經在目標位置，略過。", go);
                return false;
            }

            // 複製一份 mesh，避免修改到其他物件共用的原始資源
            Mesh newMesh = Object.Instantiate(sourceMesh);
            newMesh.name = sourceMesh.name + "_CenteredPivot";

            Vector3[] vertices = newMesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] -= pivotOffset;
            }
            newMesh.vertices = vertices;
            newMesh.RecalculateBounds();
            // 法線與 UV 不受平移影響，不需重算；Tangent/Bounds 已處理。

            MeshCollider mc = go.GetComponent<MeshCollider>();
            bool colliderSharesMesh = mc != null && mc.sharedMesh == sourceMesh;

            // 先計算世界座標下的位移補償量（用當前 rotation/scale），再套用到 Transform
            Undo.RecordObject(go.transform, "Center Pivot Transform");
            Vector3 worldOffset = go.transform.TransformVector(pivotOffset);
            go.transform.position += worldOffset;

            if (saveAsset)
            {
                string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                    Path.Combine(path, newMesh.name + ".asset"));
                AssetDatabase.CreateAsset(newMesh, assetPath);
            }

            Undo.RecordObject(mf, "Assign Centered Mesh");
            mf.sharedMesh = newMesh;

            if (colliderSharesMesh)
            {
                Undo.RecordObject(mc, "Assign Centered Collider Mesh");
                mc.sharedMesh = newMesh;
            }

            EditorUtility.SetDirty(go);
            return true;
        }
    }
}
