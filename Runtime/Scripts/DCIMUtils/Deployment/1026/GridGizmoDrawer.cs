using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VzDev.DCIMUtils
{
    /// <summary>
    /// 依照同物件上 Grid 組件的 Cell Size / Cell Gap / Cell Swizzle，
    /// 以 Gizmos.DrawWireCube 逐格繪製立方體（例：機櫃 U 槽 0.45 x 0.04445 x 1）。
    /// 僅支援 Cell Layout = Rectangle。僅 Editor 可見，不會進入 Build。
    /// </summary>
    [RequireComponent(typeof(Grid))]
    public class GridGizmoDrawer : MonoBehaviour
    {
        [Header("開關")]
        public bool drawGizmo = true;
        public bool onlyWhenSelected = false;

        [Header("繪製範圍")]
        [Tooltip("要繪製的格數（X = 欄, Y = U 數, Z = 深度格數）")]
        public Vector3Int cellCount = new Vector3Int(1, 42, 1);
        [Tooltip("起始 Cell 座標")]
        public Vector3Int startCell = Vector3Int.zero;

        [Header("顏色")]
        public Color lineColor  = new Color(1f, 1f, 1f, 0.25f);
        public Color majorColor = new Color(0.46f, 0.73f, 0f, 0.9f); // NVIDIA Green
        [Tooltip("每 N 個 U 以主色繪製，0 = 不使用")]
        [Min(0)] public int majorEvery = 10;
        [Tooltip("半透明填色，Alpha = 0 則不填")]
        public Color fillColor  = new Color(0.46f, 0.73f, 0f, 0.03f);

        [Header("U 編號")]
        public bool showLabels = true;
        [Tooltip("最下方一格的編號（機櫃一般從 U1 開始）")]
        public int labelStartIndex = 1;
        public Color labelColor = new Color(1f, 1f, 1f, 0.6f);

        private Grid _grid;
        private Grid TargetGrid => _grid != null ? _grid : (_grid = GetComponent<Grid>());

        private void OnDrawGizmos()
        {
            if (drawGizmo && !onlyWhenSelected) DrawCells();
        }

        private void OnDrawGizmosSelected()
        {
            if (drawGizmo && onlyWhenSelected) DrawCells();
        }

        private void OnValidate()
        {
            cellCount.x = Mathf.Max(1, cellCount.x);
            cellCount.y = Mathf.Max(1, cellCount.y);
            cellCount.z = Mathf.Max(1, cellCount.z);
        }

        private void DrawCells()
        {
            Grid grid = TargetGrid;
            if (grid == null || grid.cellLayout != GridLayout.CellLayout.Rectangle) return;

            Matrix4x4 prevMatrix = Gizmos.matrix;
            Color prevColor = Gizmos.color;
            Gizmos.matrix = grid.transform.localToWorldMatrix;

            Vector3 size = Grid.Swizzle(grid.cellSwizzle, grid.cellSize);
            bool drawFill = fillColor.a > 0f;

            for (int y = 0; y < cellCount.y; y++)
            {
                Color wire = (majorEvery > 0 && (y + labelStartIndex) % majorEvery == 0)
                    ? majorColor
                    : lineColor;

                for (int z = 0; z < cellCount.z; z++)
                for (int x = 0; x < cellCount.x; x++)
                {
                    Vector3 center = GetCellCenterLocal(grid,
                        new Vector3Int(startCell.x + x, startCell.y + y, startCell.z + z));

                    if (drawFill)
                    {
                        Gizmos.color = fillColor;
                        Gizmos.DrawCube(center, size);
                    }

                    Gizmos.color = wire;
                    Gizmos.DrawWireCube(center, size);
                }
            }

            Gizmos.matrix = prevMatrix;
            Gizmos.color = prevColor;

#if UNITY_EDITOR
            if (showLabels) DrawLabels(grid);
#endif
        }

        /// <summary>Cell 的 Local 中心點（已套用 Swizzle 與 Gap）。</summary>
        public static Vector3 GetCellCenterLocal(Grid grid, Vector3Int cell)
        {
            return grid.CellToLocal(cell) + Grid.Swizzle(grid.cellSwizzle, grid.cellSize * 0.5f);
        }

        /// <summary>取得指定 U（從 labelStartIndex 起算）的世界座標中心，可用於設備定位。</summary>
        public Vector3 GetUCenterWorld(int uNumber, int column = 0, int depth = 0)
        {
            Grid grid = TargetGrid;
            var cell = new Vector3Int(
                startCell.x + column,
                startCell.y + (uNumber - labelStartIndex),
                startCell.z + depth);
            return grid.transform.TransformPoint(GetCellCenterLocal(grid, cell));
        }

#if UNITY_EDITOR
        private static GUIStyle _labelStyle;

        private void DrawLabels(Grid grid)
        {
            if (_labelStyle == null)
                _labelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
            _labelStyle.normal.textColor = labelColor;
            _labelStyle.fontSize = Mathf.RoundToInt(12f * HandleUtility.GetHandleSize(transform.position) / 10f);

            // 標籤放在第一欄、第一層深度的左側邊緣
            Vector3 halfWidth = Grid.Swizzle(grid.cellSwizzle, new Vector3(grid.cellSize.x * 0.5f, 0f, 0f));

            for (int y = 0; y < cellCount.y; y++)
            {
                var cell = new Vector3Int(startCell.x, startCell.y + y, startCell.z);
                Vector3 local = GetCellCenterLocal(grid, cell) - halfWidth;
                Handles.Label(grid.transform.TransformPoint(local), $"U{y + labelStartIndex}", _labelStyle);
            }
        }
#endif
    }
}