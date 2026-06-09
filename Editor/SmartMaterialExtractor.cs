using System.IO;
using UnityEditor;
using UnityEngine;

namespace VzDev.EditorUtils
{
    public class SmartMaterialExtractor : EditorWindow
    {
        // The target directory where all extracted materials will be stored and shared
        private string targetFolder = "Assets/Materials/SharedMaterials";

        [MenuItem("VzDev Tools/BIM Pipelines/Smart Material Extractor")]
        public static void ShowWindow()
        {
            SmartMaterialExtractor window = GetWindow<SmartMaterialExtractor>("Smart Extractor");
            
            // Define the precise fixed dimensions suited for this tool's explicit layout
            Vector2 fixedSize = new Vector2(400f, 130f);
            window.minSize = fixedSize;
            window.maxSize = fixedSize;
            
            window.Show();
        }

        private void OnGUI()
        {
            // Start capturing the total rect of the content inside this vertical group
            Rect contentRect = EditorGUILayout.BeginVertical();

            GUILayout.Label("Smart Material Extractor & Merger", EditorStyles.boldLabel);
            GUILayout.Label("選取要Extract材質的模型物件，可多選", EditorStyles.boldLabel);
            GUILayout.Label("自動過濾掉多個模型物件裡有重覆名稱的材質球", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            targetFolder = EditorGUILayout.TextField("Extract Material to Folder", targetFolder);
            EditorGUILayout.Space();

            if (GUILayout.Button("Copy Material From FBXs to Folder", GUILayout.Height(30)))
            {
                ProcessSelectedFBXs();
            }

            // End the vertical group
            EditorGUILayout.EndVertical();

            // Adjust window constraints to match content size during the Repaint event
            if (Event.current.type == EventType.Repaint)
            {
                // Set a fixed aesthetic width for the tool to prevent the infinite expansion loop
                float fixedWidth = 400f; 
                float paddingHeight = 25f;
                float targetHeight = contentRect.height + paddingHeight;

                Vector2 calculatedSize = new Vector2(fixedWidth, targetHeight);
                
                // Only update if the size has actually changed to avoid redundant layout calculations
                if (this.minSize != calculatedSize)
                {
                    this.minSize = calculatedSize;
                    this.maxSize = calculatedSize;
                }
            }
        }
        
        private void ProcessSelectedFBXs()
        {
            // Ensure the target directory exists in the project
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
                AssetDatabase.Refresh();
            }

            // Get all selected FBX assets from the Project window (including sub-folders)
            Object[] selectedObjects = Selection.GetFiltered<Object>(SelectionMode.DeepAssets);
            int processedCount = 0;

            foreach (Object obj in selectedObjects)
            {
                string assetPath = AssetDatabase.GetAssetPath(obj);

                // Filter out non-FBX files
                if (string.IsNullOrEmpty(assetPath) || !assetPath.ToLower().EndsWith(".fbx"))
                    continue;

                ModelImporter modelImporter = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                if (modelImporter == null) continue;

                // Load all sub-assets embedded inside the FBX (e.g., Meshes, Materials, Animations)
                Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                bool isAssetModified = false;

                foreach (Object subAsset in subAssets)
                {
                    // We only care about embedded Material assets
                    if (subAsset is Material embeddedMaterial)
                    {
                        // Clean up special characters from Revit material names (e.g., spaces, colons)
                        string cleanName = embeddedMaterial.name.Replace(":", "_").Replace(" ", "_");
                        string expectedMatPath = Path.Combine(targetFolder, $"{cleanName}.mat");

                        // Check if the material file already exists in our shared folder
                        Material existingSharedMat = AssetDatabase.LoadAssetAtPath<Material>(expectedMatPath);

                        if (existingSharedMat != null)
                        {
                            // Case A: Material already exists. Remap the FBX to use the existing one.
                            var sourceId = new AssetImporter.SourceAssetIdentifier(embeddedMaterial);
                            modelImporter.AddRemap(sourceId, existingSharedMat);
                            isAssetModified = true;
                            Debug.Log($"[SmartExtractor] {obj.name} remapped to existing material: {cleanName}");
                        }
                        else
                        {
                            // Case B: Material is unique/new. Extract it and save it as a new .mat asset file.
                            Material extractedMat = Instantiate(embeddedMaterial);
                            extractedMat.name = cleanName;

                            // Security check to avoid path conflicts
                            expectedMatPath = AssetDatabase.GenerateUniqueAssetPath(expectedMatPath);
                            AssetDatabase.CreateAsset(extractedMat, expectedMatPath);

                            // Load the newly created asset to get its valid reference
                            Material savedSharedMat = AssetDatabase.LoadAssetAtPath<Material>(expectedMatPath);

                            // Remap the FBX to this newly extracted asset
                            var sourceId = new AssetImporter.SourceAssetIdentifier(embeddedMaterial);
                            modelImporter.AddRemap(sourceId, savedSharedMat);
                            isAssetModified = true;
                            Debug.Log($"[SmartExtractor] Extracted new unique material: {cleanName}");
                        }
                    }
                }

                // If any remapping occurred, save the changes and reimport the FBX to apply them
                if (isAssetModified)
                {
                    EditorUtility.SetDirty(modelImporter);
                    modelImporter.SaveAndReimport();
                    processedCount++;
                }
            }

            // Refresh the AssetDatabase to display new files in the Project view immediately
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", $"Successfully processed {processedCount} FBX models.", "OK");
        }
    }
}