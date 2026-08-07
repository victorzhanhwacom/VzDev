#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace VzDev.DrawUtils
{
    [CustomEditor(typeof(GizmoDrawer))]
    public class GizmoDrawerEditor : Editor
    {
        private BoxBoundsHandle boxHandle = new BoxBoundsHandle();

        private void OnSceneGUI()
        {
            var t = (GizmoDrawer)target;

            switch (t.style)
            {
                case GizmoStyle.SolidSphere:
                case GizmoStyle.WireSphere:
                    DrawRadiusHandle(t);
                    break;
                case GizmoStyle.SolidCube:
                case GizmoStyle.WireCube:
                    DrawCubeSizeHandle(t);
                    break;
            }
        }

        private void DrawRadiusHandle(GizmoDrawer t)
        {
            EditorGUI.BeginChangeCheck();
            float newRadius = Handles.RadiusHandle(Quaternion.identity, t.transform.position, t.radius);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(t, "Change Radius");
                t.radius = newRadius;
            }
        }

        private void DrawCubeSizeHandle(GizmoDrawer t)
        {
            boxHandle.center = Vector3.zero;
            boxHandle.size = t.cubeSize;

            using (new Handles.DrawingScope(t.transform.localToWorldMatrix))
            {
                EditorGUI.BeginChangeCheck();
                boxHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(t, "Change Cube Size");
                    t.cubeSize = boxHandle.size;
                    SceneView.RepaintAll();
                }
            }
        }
    }
}
#endif