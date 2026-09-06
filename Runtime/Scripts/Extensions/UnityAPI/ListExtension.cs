using System;
using System.Collections.Generic;
using System.Linq;
using VzDev.DebugUtils;
using Newtonsoft.Json;
using UnityEngine;
using Debug = VzDev.ToolUtils.Debug;
using VzDev.NetLibrary.Extensions;
using static VzDev.UnityAPI.Extensions.TransformExtension;
using System.Text;

namespace VzDev.ApiExtensions
{
    /// 原API類別功能擴充
    public static class ListExtension
    {

        /// <summary>
        /// [Extended] - 將List{T}轉成字串，並以separator隔開
        /// </summary>
        public static string CombineToString<T>(this List<T> self, bool isLineBreak = false, string separator = ",") where T : Component
        {
            var result = new StringBuilder();
            foreach (T t in self)
            {
                result.Append(t.name);
                if (isLineBreak) result.AppendLine();
                else result.Append(separator);
            }
            return result.ToString();
        }


        /// <summary>
        /// [Extended] - collectoin與self比對，將不重複的元素加入List中
        /// </summary>
        public static void AddRangeWithDistinct<T>(this List<T> self, IEnumerable<T> collection)
        {
            Debug.Assert(self != null, "List is null. Cannot add range.");
            HashSet<T> set = new HashSet<T>(self);
            foreach (var item in collection)
            {
                if (set.Add(item))
                {
                    self.Add(item);
                }
            }
        }

        /// <summary>
        /// [Extended] - 移除所有子物件
        /// </summary>
        public static void RemoveAllChildren(this Transform self)
        {
            for (int i = self.childCount - 1; i >= 0; i--)
            {
                ObjectHelper.Destroy(self.GetChild(i).gameObject);
            }
        }

        ////////////////////////////////////////////////////////////////////


        /// [Extended] - 將List{KeyValueData{string, string}}轉成JSON字串格式
        public static string ToJsonFormat(this List<KeyValueData<string, string>> self)
        {
            var dict = self.ToDictionary(x => x.Key, x => x.Value);
            return JsonConvert.SerializeObject(dict, Formatting.Indented);
        }


        /// [Extended] - 依TEnum類型來分類，存在Dictionary{Enum類型, 數量}
        public static Dictionary<TEnum, int> GroupCount<TEnum, TClass>(this List<TClass> self, Func<TClass, TEnum> selector)
        {
            // 預先把所有 Enum 值建立出來 → 不會字典擴容 → 避免 GC
            TEnum[] enumValues = (TEnum[])Enum.GetValues(typeof(TEnum));
            Dictionary<TEnum, int> result = new Dictionary<TEnum, int>(enumValues.Length);
            for (int i = 0; i < enumValues.Length; i++)
                result[enumValues[i]] = 0;
            // 極速 for 迴圈計數
            for (int i = 0; i < self.Count; i++)
            {
                var key = selector(self[i]);
                result[key]++;
            }

            return result;
        }

        /// [Extended] -  列出所有元素
        public static string ToPrint<T>(this List<T> self) => string.Join(", ", self);

        /// [Extended] - 移除Missing項目
        public static List<T> ClearMissingTargets<T>(this List<T> self)
        {
            self.RemoveAll(item => item == null);
            return self;
        }

        ///<summary>
        /// [Extended] - 嘗試將元素加入List中，若已存在則不加入
        ///</summary>        
        public static bool TryAdd<T>(this List<T> self, T target) where T : Component
        {
            bool result = self.Contains(target);
            if (result == false) self.Add(target);
            return result;
        }

        /// [Extended] - 依照Key值取得Value
        public static bool TyrGetValue<T>(this List<KeyValueData<string, T>> self, string key, out T value)
        {
            var result = self.FirstOrDefault(kvp => kvp.Key == key);
            value = result != null ? result.Value : default(T);
            return result != null;
        }

        #region 依關鍵字進行過濾
        /// [Extended] -  取得Name包含關鍵字的對像 (含有單字)
        public static List<TComponent> FilterByNameForKeywords<TComponent>(this List<TComponent> self,
           EnumSearchType searchType = EnumSearchType.Include, params string[] keyWords)
            where TComponent : Component
        {
            bool isInclude = searchType == EnumSearchType.Include;
            return self.Where(target =>
                    keyWords.Any(word => target.name.ContainKeyword(StringComparison.OrdinalIgnoreCase, word) == isInclude))
                .ToList();
        }

        /// [Extended] -  取得Name包含關鍵字的對像 (含有字元)
        public static List<TComponent> FilterByNameForKeyChars<TComponent>(this List<TComponent> self,
            EnumSearchType searchType = EnumSearchType.Include, params string[] keyWords)
            where TComponent : Component
        {
            bool isInclude = searchType == EnumSearchType.Include;
            return self.Where(target =>
                    keyWords.Any(word => target.name.Contains(word) == isInclude))
                .ToList();
        }
        #endregion

        /// [Extended] - List是否為Null或Empty
        public static bool IsNullOrEmpty<T>(this List<T> self) => self == null || self.Count == 0;

        /// [Extended] - 複製一份List
        public static List<T> MakeCopyList<T>(this List<T> self) => self.Select(x => x).ToList();

        /// [Extended] - 以separator隔開，將數組全部列出來
        public static string PrintAll<T>(this List<T> self, string separator = ",") => string.Join(separator, self);

        /// [Extended] - 篩選有實作IReceiveData<TData>的對像，傳送TData資料給這些對像
        public static void ReceiveData<T, TData>(this List<T> self, TData data)
        {
            /*      foreach (IReceiveData<TData> target in self.OfType<IReceiveData<TData>>())
                 {
                     target.ReceiveData(data);
                 } */
        }

        /// [Extended] - 篩選出 MonoBehaviour List 中所有實作了TData類別或介面的元素
        public static List<MonoBehaviour> FilterByType<TData>(this List<MonoBehaviour> self)
        {
            List<MonoBehaviour> result = new List<MonoBehaviour>();
            for (int i = 0; i < self.Count; i++)
            {
                if (self[i] is TData) result.Add(self[i]);
                else Debug.LogWarning($"{self[i].name} 並無實作 {typeof(TData).Name}");
            }

            return result;
        }

        /// [Extended] - 替換Renderer的Texture
        public static void ReplaceTexture(this List<MeshRenderer> self, Texture texture)
        {
            if (self == null || self.Count == 0 || texture == null) return;

            // 找到第一個有效的Renderer材質作為基底
            Material baseMat = null;
            foreach (var r in self)
            {
                if (r != null && r.sharedMaterial != null)
                {
                    baseMat = r.sharedMaterial;
                    break;
                }
            }

            if (baseMat == null)
                return;

            // 創建一個新的材質實例並設定Texture
            Material sharedInstance = new Material(baseMat) { mainTexture = texture };

            // 給所有Renderer套用這個新材質實例
            foreach (var r in self)
            {
                if (r != null)
                    r.sharedMaterial = sharedInstance;
            }
        }

        /// [Extended] - 替換Renderer的Texture
        public static void ReplaceTexture(this MeshRenderer[] self, Texture texture) =>
            ReplaceTexture(self.ToList(), texture);
    }
}