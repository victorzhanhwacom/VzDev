using System.Runtime.CompilerServices;
using UnityEngine;

namespace VzDev.ToolUtils
{
    public static class FrameGateExtensions
    {
        /// <summary>
        /// 同一幀內只會回傳一次 true。
        /// lastFrame 初始值請設為 -1，第 0 幀才能正常通過。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckFrame(this ref int lastFrame)
        {
            int frame = Time.frameCount;
            if (lastFrame == frame) return false;
            lastFrame = frame;
            return true;
        }

        /// <summary>
        /// 每隔 interval 幀才回傳一次 true（節流用）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckFrameInterval(this ref int lastFrame, int interval=20)
        {
            int frame = Time.frameCount;
            if (frame - lastFrame < interval) return false;
            lastFrame = frame;
            return true;
        }
    }
}