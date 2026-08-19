using NaughtyAttributes;
using UnityEngine;

namespace VzDev.DrawUtils
{
    public class GizmoDrawer : MonoBehaviour
    {
        #region Fields
        public bool alwaysShow = false;
        [Foldout("[Settings]")] public Color color = Color.yellow;
        [Foldout("[Settings]")] public GizmoStyle style = GizmoStyle.SolidSphere;
        [Foldout("[Settings]")] public float radius = 0.5f;
        [Foldout("[Settings]")] public Vector3 cubeSize = Vector3.one * 0.5f;
        #endregion
        
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (alwaysShow) DrawGizmo();
        }

        private void OnDrawGizmosSelected()
        {
            if (!alwaysShow) DrawGizmo();
        }

        private void DrawGizmo()
        {
            Gizmos.color = color;

            switch (style)
            {
                case GizmoStyle.SolidSphere:
                    Gizmos.DrawSphere(transform.position, radius);
                    break;
                case GizmoStyle.WireSphere:
                    Gizmos.DrawWireSphere(transform.position, radius);
                    break;
                case GizmoStyle.SolidCube:
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawCube(Vector3.zero, cubeSize);
                    Gizmos.matrix = Matrix4x4.identity;
                    break;
                case GizmoStyle.WireCube:
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(Vector3.zero, cubeSize);
                    Gizmos.matrix = Matrix4x4.identity;
                    break;
            }
        }
#endif
    }

    public enum GizmoStyle
    {
        SolidSphere,
        WireSphere,
        SolidCube,
        WireCube,
    }
}