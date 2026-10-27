using System;
using System.Runtime.CompilerServices;

namespace VzDev.ToolUtils
{
    /// <summary>
    /// 奇偶數判定。以位元運算 (n & 1) 實作，不經除法，負數亦正確。
    /// </summary>
    public static class ParityExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool IsOdd(this int n)   => (n & 1) != 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool IsOdd(this long n)  => (n & 1L) != 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool IsOdd(this uint n)  => (n & 1u) != 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool IsOdd(this ulong n) => (n & 1UL) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool IsEven(this int n)   => (n & 1) == 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool IsEven(this long n)  => (n & 1L) == 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool IsEven(this uint n)  => (n & 1u) == 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static bool IsEven(this ulong n) => (n & 1UL) == 0;

        /// <summary>
        /// 集合中是否含有奇數。不使用 LINQ，零 GC。
        /// 可直接傳入 int[] 或 List 轉成的 Span。
        /// </summary>
        public static bool HasOdd(this ReadOnlySpan<int> values)
        {
            for (int i = 0; i < values.Length; i++)
                if ((values[i] & 1) != 0) return true;
            return false;
        }

        public static bool HasOdd(this int[] values) => values != null && HasOdd((ReadOnlySpan<int>)values);
    }
}
