using UnityEngine;

namespace VzDev.ApiExtensions
{
    /// [Extended] 原API類別功能擴充
    public static class VectorExtension
    {

        /// <summary>
        /// 判斷兩個 Vector3 是否相近（threshold 為平方誤差值，需自行預先計算避免重複運算）。
        /// </summary>
        public static bool IsApproximatelySqr(this Vector3 a, Vector3 b, float thresholdSqr)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            float dz = a.z - b.z;
            return (dx * dx + dy * dy + dz * dz) < thresholdSqr;
        }
        /// <summary>
        /// 一般用法，內部會計算 threshold 平方（適合非高頻呼叫場景）。
        /// </summary>
        public static bool IsApproximately(this Vector3 a, Vector3 b, float threshold = 0.001f)
            => a.IsApproximatelySqr(b, threshold * threshold);
        //////////////////////////////////////////////////////////////////////////////////////////////////

        #region Vector3

        /// [Extended] XY值對調
        public static Vector3 SwapXZ(this Vector3 self) => new(self.z, self.y, self.x);


        /// [Extended] 轉換成整數值
        public static Vector3 ToVectorInt(this Vector3 self) => new(Mathf.Round(self.x), Mathf.Round(self.y), Mathf.Round(self.z));

        /// [Extended] [Extended] 設定 X 值
        public static Vector3 SetX(this Vector3 self, float x) => new Vector3(x, self.y, self.z);

        /// [Extended] [Extended] 設定 Y 值
        public static Vector3 SetY(this Vector3 self, float y) => new Vector3(self.x, y, self.z);

        /// [Extended] [Extended] 設定 Z 值
        public static Vector3 SetZ(this Vector3 self, float z) => new Vector3(self.x, self.y, z);


        /// [Extended] [Extended] 增加 X 值
        public static Vector3 AddX(this Vector3 self, float x) => AddValue(self, x, 0, 0);

        /// [Extended] [Extended] 增加 Y 值
        public static Vector3 AddY(this Vector3 self, float y) => AddValue(self, 0, y, 0);

        /// [Extended] [Extended] 增加 Z 值
        public static Vector3 AddZ(this Vector3 self, float z) => AddValue(self, 0, 0, z);

        /// [Extended] [Extended] - 增加XYZ值 (Vector3為struct, 無法直接更改其值)
        public static Vector3 AddValue(this Vector3 self, float x = 0, float y = 0, float z = 0) =>
            new(self.x + x, self.y + y, self.z + z);


        /// [Extended] [Extended] 乘上倍率 X 值
        public static Vector3 MultiplyX(this Vector3 self, float x) => Multiply(self, x, 1, 1);

        /// [Extended] [Extended] 乘上倍率 Y 值
        public static Vector3 MultiplyY(this Vector3 self, float y) => Multiply(self, 1, y, 1);

        /// [Extended] [Extended] 乘上倍率 Z 值
        public static Vector3 MultiplyZ(this Vector3 self, float z) => Multiply(self, 1, 1, z);

        /// [Extended] [Extended] 乘上倍率
        public static Vector3 Multiply(this Vector3 self, float x = 1, float y = 1, float z = 1)
            => new Vector3(self.x * x, self.y * y, self.z * z);

        /// [Extended] [Extended] Clamp 限制範圍
        public static Vector3 Clamp(this Vector3 self, Vector3 min, Vector3 max)
            => new Vector3(
                Mathf.Clamp(self.x, min.x, max.x),
                Mathf.Clamp(self.y, min.y, max.y),
                Mathf.Clamp(self.z, min.z, max.z)
            );

        #endregion

        #region Vector2

        /// [Extended] X、Y之間的差值
        public static float Difference(this Vector2 self) => Mathf.Abs(self.x - self.y);

        /// [Extended] 從X到Y之間取得亂數值
        public static float GetRandomValue(this Vector2 self)
        {
            float min = Mathf.Min(self.x, self.y);
            float max = Mathf.Max(self.x, self.y);
            return Random.Range(min, max);
        }

        /// [Extended] 計算值於XY之間的百分比
        public static float GetPercentage01(this Vector2 self, float value)
        {
            // 避免除以 0
            if (Mathf.Approximately(self.y, self.x)) return 0f;
            return Mathf.InverseLerp(self.x, self.y, value);
        }

        /// [Extended] XY值差值
        public static float GetDeviation(this Vector2 self) => Mathf.Abs(self.x - self.y);


        /// [Extended] XY值對調
        public static Vector2 SwapXY(this Vector2 self) => new(self.y, self.x);

        /// [Extended] 轉換成整數值
        public static Vector2 ToVectorInt(this Vector2 self) => new(Mathf.Round(self.x), Mathf.Round(self.y));

        /// [Extended] 尺吋
        public static Vector2 SizeData(this Texture2D texture)
        {
            return new Vector2(texture.width, texture.height);
        }

        /// [Extended] [Extended] 設定 X 值
        public static Vector2 SetX(this Vector2 self, float x) => new Vector2(x, self.y);

        /// [Extended] [Extended] 設定 Y 值
        public static Vector2 SetY(this Vector2 self, float y) => new Vector2(self.x, y);

        /// [Extended] [Extended] 增加 X 值
        public static Vector2 AddX(this Vector2 self, float x) => self.AddValue(x, 0);

        /// [Extended] [Extended] 增加 Y 值
        public static Vector2 AddY(this Vector2 self, float y) => self.AddValue(0, y);

        /// [Extended] [Extended] 增加 XY 值
        public static Vector2 AddValue(this Vector2 self, float x = 0, float y = 0) =>
            new Vector2(self.x + x, self.y + y);

        /// [Extended] [Extended] 乘上倍率 X 值
        public static Vector2 MultiplyX(this Vector2 self, float x) => new Vector2(self.x * x, self.y);

        /// [Extended] [Extended] 乘上倍率 Y 值
        public static Vector2 MultiplyY(this Vector2 self, float y) => new Vector2(self.x, self.y * y);

        /// [Extended] [Extended] 乘上倍率 XY 值
        public static Vector2 Multiply(this Vector2 self, float x = 1, float y = 1) =>
            new Vector2(self.x * x, self.y * y);

        public static Vector2 Clamp(this Vector2 self, Vector2 min, Vector2 max) =>
            new Vector2(Mathf.Clamp(self.x, min.x, max.x), Mathf.Clamp(self.y, min.y, max.y));

        public static Vector2 Abs(this Vector2 self) => new Vector2(Mathf.Abs(self.x), Mathf.Abs(self.y));
        public static Vector2 Round(this Vector2 self) => new Vector2(Mathf.Round(self.x), Mathf.Round(self.y));
        public static Vector2 Floor(this Vector2 self) => new Vector2(Mathf.Floor(self.x), Mathf.Floor(self.y));
        public static Vector2 Ceil(this Vector2 self) => new Vector2(Mathf.Ceil(self.x), Mathf.Ceil(self.y));

        #endregion

        #region Vector3Int
        /// [Extended] [Extended] Clamp 限制範圍
        public static Vector3Int ToClamp(this Vector3Int self, Vector3Int min, Vector3Int max)
            => self.ClampX(min.x, max.x).ClampY(min.y, max.y).ClampZ(min.z, max.z);

        /// [Extended] [Extended] ClampX 限制範圍
        public static Vector3Int ClampX(this Vector3Int self, int min, int max)
            => new Vector3Int(Mathf.Clamp(self.x, min, max), self.y, self.z);

        /// [Extended] [Extended] ClampY 限制範圍
        public static Vector3Int ClampY(this Vector3Int self, int min, int max)
            => new Vector3Int(self.x, Mathf.Clamp(self.y, min, max), self.z);

        /// [Extended] [Extended] ClampZ 限制範圍
        public static Vector3Int ClampZ(this Vector3Int self, int min, int max)
            => new Vector3Int(self.x, self.y, Mathf.Clamp(self.z, min, max));


        /// [Extended] [Extended] 設定 X 值
        public static Vector3Int SetX(this Vector3Int self, int x) => new Vector3Int(x, self.y, self.z);

        /// [Extended] [Extended] 設定 Y 值
        public static Vector3Int SetY(this Vector3Int self, int y) => new Vector3Int(self.x, y, self.z);

        /// [Extended] [Extended] 設定 Z 值
        public static Vector3Int SetZ(this Vector3Int self, int z) => new Vector3Int(self.x, self.y, z);


        /// [Extended] [Extended] 增加 X 值
        public static Vector3Int AddX(this Vector3Int self, int x) => AddValue(self, x, 0, 0);

        /// [Extended] [Extended] 增加 Y 值
        public static Vector3Int AddY(this Vector3Int self, int y) => AddValue(self, 0, y, 0);

        /// [Extended] [Extended] 增加 Z 值
        public static Vector3Int AddZ(this Vector3Int self, int z) => AddValue(self, 0, 0, z);

        /// [Extended] [Extended] - 增加XYZ值 (Vector3Int為struct, 無法直接更改其值)
        public static Vector3Int AddValue(this Vector3Int self, int x = 0, int y = 0, int z = 0) =>
            new(self.x + x, self.y + y, self.z + z);
        #endregion
    }
}