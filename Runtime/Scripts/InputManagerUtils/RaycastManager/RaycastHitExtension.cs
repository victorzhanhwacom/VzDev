using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VzDev.ApiExtensions
{
    /// <summary>
    /// [Extended] - 原API類別功能擴充
    /// </summary>
    public static class RaycastExtension
    {
        /// <summary>
        /// [Extended] - 複製一份轉為TransformList
        /// </summary>
        public static List<Transform> CloneToTransformList(this RaycastHit[] self) => self.CloneToTransformArray().ToList();

        /// <summary>
        /// [Extended] - 複製一份轉為Transform[]
        /// </summary>
        public static Transform[] CloneToTransformArray(this RaycastHit[] self, int size = 10)
        {
            Transform[] result = new Transform[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = self[i].collider.gameObject.transform;
            }
            return result;
        }

        /// <summary>
        /// [Extended] - 與ray.origin座標之距離進行排序 (是否遞減排序)
        /// </summary>
        public static void SortByOriginDistance(this RaycastHit[] self, int size, bool isDesc = false)
        {
            Array.Sort(self, 0, size, Comparer<RaycastHit>.Create(
                (a, b) => isDesc ? b.distance.CompareTo(a.distance) : a.distance.CompareTo(b.distance)
            ));
        }
    }
}