using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Pool;
using VzDev.NetLibrary.Extensions;
using Object = UnityEngine.Object;

namespace VzDev.UnityAPI.Extensions
{
    /// [Extended] 原API類別功能擴充
    public static class TransformExtension
    {
        private static readonly StringBuilder _stringBuilder = new StringBuilder(256);

        /// <summary>
        /// Destroy刪除此GameObject
        /// </summary>
        public static void Destroy(this Transform self)
        {
            // 1. 安全檢查：防止對已經是 null 的物件進行操作
            if (self == null || self.gameObject == null)
            {
                Debug.LogWarning("Attempted to destroy a null GameObject. Operation skipped.");
                return;
            }
            // 2. 只要是在播放狀態（不論是打包後還是編輯器內），都用 Destroy
            if (Application.isPlaying)
            {
                Object.Destroy(self.gameObject);
            }
            else
            {
                // 3. 在純編輯器模式下（非播放狀態）
#if UNITY_EDITOR
                // 如果是編輯器環境，加入 Undo 紀錄，讓 Ctrl+Z 可以回復
                UnityEditor.Undo.DestroyObjectImmediate(self.gameObject);
#else
                // 雖然打包後非 Play 狀態幾乎不可能觸發，但保留作為保險
                Object.DestroyImmediate(self.gameObject);
#endif
            }
        }

        /// <summary>
        /// 以模型底部為Pivot去設置Position（適用於Pivot在中心點的模型）
        /// </summary>
        public static void SetPositionByBottom(this Transform self, Vector3 targetBottomPos, float extraYOffset = 0f)
        {
            if (self.TryGetComponent(out Renderer renderer))
            {
                // 1. 動態計算標準的底部偏移量（通用邏輯）
                Bounds bounds = renderer.bounds;
                float bottomOffsetY = bounds.center.y - bounds.min.y;

                // 2. 把目標位置 + 標準偏移量 + 外部微調量
                // 注意：因為你原本的 Revit 髒 code 是「減去」0.0445f * 0.5f，所以這邊加上 extraYOffset，外部傳負數即可。
                self.position = targetBottomPos + Vector3.up * (bottomOffsetY + extraYOffset);
            }
            else
            {
                // 如果沒有 Renderer，就退化成一般設定位置，並跳警告
                Debug.LogWarning($"[SetPositionByBottom] 物件 {self.name} 找不到 Renderer，改用預設 Position 設定。");
            }
        }

        #region 取得底下所有子物件
        /// <summary>
        /// 尋找所有的子物件 ()
        /// <para>+ 每次呼叫時，會new List以產生GC Alloc，所以要避免在 Update 中頻繁呼叫</para>
        /// <para>+ For少數呼叫</para>
        /// </summary>
        public static List<Transform> GetAllChildren(this List<Transform> self, bool includeInactive = true)
        {
            // 安全檢查
            if (self == null || self.Count == 0) return new List<Transform>();

            List<Transform> result = new List<Transform>(); // 造成GC Alloc，但因為是少數呼叫，所以接受

            for (int i = 0; i < self.Count; i++)
            {
                Transform parent = self[i];
                if (parent == null) continue;

                // 1. 呼叫 Unity 官方優化過的極速搜查 API（true 代表包含隱藏物件）
                Transform[] allComponents = parent.GetComponentsInChildren<Transform>(includeInactive);

                // 2. 因為此 API 包含 parent 自己（在索引 0），我們從索引 1 開始抓，就能完美避開自己
                for (int j = 1; j < allComponents.Length; j++)
                {
                    result.Add(allComponents[j]);
                }
            }

            return result;
        }


        /// <summary>
        /// 零配置優化版：將所有子物件填充到傳入的 outputList 中，完全不產生記憶體垃圾 ()
        /// <para>+ 因為已經有outputList，所以不會產生額外的記憶體分配</para>
        /// <para>+ For Update頻繁呼叫</para>
        /// </summary>
        public static void GetAllChildrenNonAlloc(this List<Transform> self, List<Transform> outputList, bool includeInactive = true)
        {
            if (self == null || outputList == null) return;

            // 確保容器是乾淨的，但保留原本的記憶體容量（Capacity)，避免每次重複 new List 產生的 GC Alloc
            outputList.Clear();

            for (int i = 0; i < self.Count; i++)
            {
                if (self[i] != null)
                {
                    CollectRecursive(self[i], outputList, includeInactive);
                }
            }
        }

        /// <summary>
        /// 用 for 迴圈與 childCount 取代 foreach，完全消滅迭代器垃圾
        /// <para>+ 若寫成區域函式會觸發「閉包（Closure）」的機制，造成每次呼叫都會產生一個新的 delegate 實例，反而增加 GC Alloc；改成 private static 方法，就不會有這問題了！</para>
        /// </summary>
        private static void CollectRecursive(Transform parent, List<Transform> output, bool includeInactive = true)
        {
            int count = parent.childCount;
            for (int i = 0; i < count; i++)
            {
                Transform child = parent.GetChild(i);

                // 核心邏輯：如果不包含隱藏物件，且這個子物件本身是隱藏的 (activeSelf == false)，就直接跳過它和它的所有子孫！
                if (!includeInactive && !child.gameObject.activeSelf)
                {
                    continue;
                }

                // 符合條件，加入清單
                output.Add(child);

                // 繼續往下層遞迴
                if (child.childCount > 0)
                {
                    CollectRecursive(child, output, includeInactive);
                }
            }
        }
        #endregion

        #region 新增元件
        /// <summary>
        /// 嘗試新增元件，如果已經有了就直接回傳現有的元件；如果沒有則新增並回傳，並且回傳值的布林值代表是否是新建立的（true = 新建立的，false = 原本就有的）
        /// </summary>
        public static bool TryAddComponent<TComponent>(this Transform self, out TComponent component) where TComponent : Component => TryAddComponent(self.gameObject, out component);

        /// <summary>
        /// 嘗試新增元件，如果已經有了就直接回傳現有的元件；如果沒有則新增並回傳，並且回傳值的布林值代表是否是新建立的（true = 新建立的，false = 原本就有的）
        /// </summary>
        public static bool TryAddComponent<TComponent>(this GameObject self) where TComponent : Component 
        => TryAddComponent(self, out TComponent component);
        /// <summary>
        /// 嘗試新增元件，如果已經有了就直接回傳現有的元件；如果沒有則新增並回傳，並且回傳值的布林值代表是否是新建立的（true = 新建立的，false = 原本就有的）
        /// </summary>
        public static bool TryAddComponent<TComponent>(this GameObject self, out TComponent component) where TComponent : Component
        {
            if (!self.TryGetComponent(out component))
            {
                component = self.AddComponent<TComponent>();
                return true; // 代表是新建立的
            }
            return false; // 代表原本就有了
        }
        
        #endregion

        #region 包含子物件的判斷
        /// <summary>
        /// 判斷目標物件是否在此物件的階層下（包含子物件、孫物件等所有下層階層）
        /// </summary>
        public static bool ContainsInHierarchy<T>(this Transform self, T target) where T : Component
        {
            if (target == null)
            {
                Debug.LogWarning("ContainsInHierarchy: Target is null, returning false.");
                return false;
            }
            // IsChildOf 會檢查整個下游階層（包含自己，所以排除自己）
            return target.transform != self && target.transform.IsChildOf(self);
        }

        /// <summary>
        /// 判斷目標物件是否為此物件的直接子物件（只包含下一層階層，不包含孫物件等更下層階層）
        /// </summary>
        public static bool ContainsChild<T>(this Transform self, T target) where T : Component
        {
            if (target == null)
            {
                Debug.LogWarning("ContainsChild: Target is null, returning false.");
                return false;
            }

            // 直接檢查目標的父物件是不是自己
            return target.transform.parent == self;
        }
        #endregion

        #region 取得階層路徑
        // 用來儲存所有物件路徑的快取池
        private static readonly Dictionary<int, string> _pathCache = new Dictionary<int, string>();

        /// <summary>
        /// 獲取階層路徑（保證 0 GC 讀取）
        /// </summary>
        public static string GetHierarchyPath(this Transform self)
        {
            if (self == null) return string.Empty;

            // 使用 InstanceID 作為 Key，這是唯一的整數，比對速度極快
            int id = self.GetInstanceID();

            // 如果快取裡面已經有了，直接返回！(0 GC Alloc, O(1) 複雜度)
            if (_pathCache.TryGetValue(id, out string cachedPath))
            {
                return cachedPath;
            }

            // 如果快取沒有，才「初始化」計算一次（只有這一次會產生 GC）
            string generatedPath = CalculatePathInternal(self);
            _pathCache[id] = generatedPath;

            return generatedPath;
        }

        /// <summary>
        /// 當物件在階層中被移動（Change Parent）或改名時，必須手動清除快取
        /// </summary>
        public static void ClearPathCache(this Transform self)
        {
            if (self == null) return;

            // 清除自己與所有子物件的快取
            int id = self.GetInstanceID();
            _pathCache.Remove(id);

            foreach (Transform child in self)
            {
                child.ClearPathCache();
            }
        }

        // 內部計算邏輯（僅在快取失效時執行）
        private static string CalculatePathInternal(Transform target)
        {
            _stringBuilder.Length = 0;
            _stringBuilder.Append(target.name);

            Transform current = target.parent;
            while (current != null)
            {
                // 這裡使用 Insert 雖然有效能耗損，但因為只在初始化執行一次，所以完全可以接受
                _stringBuilder.Insert(0, "/");
                _stringBuilder.Insert(0, current.name);
                current = current.parent;
            }

            return _stringBuilder.ToString();
        }
        #endregion

        #region 依關鍵字搜尋子物件

        /// <summary>
        /// 【方便版】搜尋子孫物件 (不限元件類型)，名稱(包含/不包含)關鍵字
        /// </summary>
        public static List<Transform> FindChildrenByKeyword(this Transform self, string keyword,
            EnumSearchType searchType = EnumSearchType.Include, bool includeInactive = true)
        {
            var results = new List<Transform>();
            self.FindChildrenByKeyword(results, keyword, searchType, includeInactive);
            return results;
        }

        /// <summary>
        /// 【極致優化 - 單一關鍵字版】搜尋子物件 (不限元件類型)，100% 0 GC Alloc
        /// </summary>
        public static void FindChildrenByKeyword(this Transform self, List<Transform> results,
            string keyword, EnumSearchType searchType = EnumSearchType.Include, bool includeInactive = true)
        {
            if (results == null) return;
            if (string.IsNullOrEmpty(keyword))
            {
                Debug.LogWarning("FindChildrenByKeyword: Keyword is null or empty.");
                return;
            }

            List<Transform> transList = ListPool<Transform>.Get();
            self.GetComponentsInChildren(includeInactive: includeInactive, transList);

            int count = transList.Count;
            for (int i = 0; i < count; i++)
            {
                Transform target = transList[i];
                if (target == null || target == self) continue;

                if (!includeInactive && !target.gameObject.activeInHierarchy) continue;

                // 這裡直接比對單一字串，不透過陣列，完全 0 GC！
                if (IsMatchSingle(target.name, searchType, keyword))
                {
                    results.Add(target);
                }
            }

            ListPool<Transform>.Release(transList);
        }

        /// <summary>
        /// 【方便版】搜尋子孫物件(有實作 T 類別)，名稱(包含/不包含)關鍵字
        /// </summary>
        public static List<Transform> FindChildrenByKeyword<T>(this Transform self, string keyword,
            EnumSearchType searchType = EnumSearchType.Include, bool includeInactive = true) where T : Component
        {
            var results = new List<Transform>();
            self.FindChildrenByKeyword<T>(results, keyword, searchType, includeInactive);
            return results;
        }

        /// <summary>
        /// 【極致優化 - 單一關鍵字版】搜尋子物件(有實作 T 類別)，100% 0 GC Alloc
        /// </summary>
        public static void FindChildrenByKeyword<T>(this Transform self, List<Transform> results,
            string keyword, EnumSearchType searchType = EnumSearchType.Include, bool includeInactive = true) where T : Component
        {
            if (results == null) return;
            if (string.IsNullOrEmpty(keyword))
            {
                Debug.LogWarning("FindChildrenByKeyword: Keyword is null or empty.");
                return;
            }

            List<T> compList = ListPool<T>.Get();
            HashSet<Transform> seenTransforms = HashSetPool<Transform>.Get();

            self.GetComponentsInChildren(includeInactive: includeInactive, compList);

            int count = compList.Count;
            for (int i = 0; i < count; i++)
            {
                T comp = compList[i];
                if (comp == null) continue;

                Transform target = comp.transform;
                if (target == self) continue;

                if (!includeInactive)
                {
                    bool isEnabled = comp switch
                    {
                        Behaviour b => b.enabled,
                        Collider c => c.enabled,
                        Renderer r => r.enabled,
                        _ => true
                    };
                    if (!isEnabled || !target.gameObject.activeInHierarchy) continue;
                }

                if (!seenTransforms.Add(target)) continue;

                // 這裡直接比對單一字串，不透過陣列，完全 0 GC！
                if (IsMatchSingle(target.name, searchType, keyword))
                {
                    results.Add(target);
                }
            }

            HashSetPool<Transform>.Release(seenTransforms);
            ListPool<T>.Release(compList);
        }

        /// <summary>
        /// 【效能優化版】針對單一關鍵字的比對，避免 ContainKeyword 內部的迴圈和陣列存取，直接使用 string.Contains 的優化版本（0 GC Alloc）
        /// </summary>
        private static bool IsMatchSingle(string name, EnumSearchType searchType, string keyword)
        {
            // 假設你有針對單一字串的 ContainKeyword 擴充方法，或直接使用 name.Contains()
            bool isContains = name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
            return searchType switch
            {
                EnumSearchType.Include => isContains,
                EnumSearchType.Exclude => !isContains,
                _ => false
            };
        }

        /// <summary>
        /// 【效能優化版】搜尋子孫物件 (不限元件類型)，將結果填入傳入的 List 中，徹底達到 0 GC Alloc
        /// </summary>
        public static void FindChildrenByKeywords(this Transform self, List<Transform> results,
            EnumSearchType searchType = EnumSearchType.Include,
            bool includeInactive = true,
            params string[] keywords)
        {
            if (results == null) return;
            if (keywords == null || keywords.Length == 0)
            {
                Debug.LogWarning("FindChildrenByKeywords: No keywords provided.");
                return;
            }

            // 獲取暫存 List
            List<Transform> transList = ListPool<Transform>.Get();
            self.GetComponentsInChildren(includeInactive: includeInactive, transList);

            int count = transList.Count;
            for (int i = 0; i < count; i++)
            {
                Transform target = transList[i];
                if (target == null || target == self) continue;

                // 不限型別時，若 includeInactive = false，需檢查整個 GameObject 是否在階層中啟用
                if (!includeInactive && !target.gameObject.activeInHierarchy) continue;

                // 關鍵字比對
                if (IsMatch(target.name, searchType, keywords))
                {
                    results.Add(target);
                }
            }

            // 釋放物件池
            ListPool<Transform>.Release(transList);
        }

        /// <summary>
        /// 【方便版】搜尋子孫物件(有實作 T 類別)，名稱(包含/不包含)關鍵字
        /// <para>+ 內部會自己 new List 回傳，所以會有 GC Alloc，適合少數呼叫</para>
        /// </summary>
        public static List<Transform> FindChildrenByKeywords<T>(this Transform self,
            EnumSearchType searchType = EnumSearchType.Include, bool includeInactive = true, params string[] keywords) where T : Component
        {
            var results = new List<Transform>();
            self.FindChildrenByKeywords<T>(results, searchType, includeInactive, keywords);
            return results;
        }

        /// <summary>
        /// 【效能優化版】搜尋子物件(有實作 T 類別)，將結果填入傳入的 results 中，徹底達到 0 GC Alloc
        /// </summary>
        public static void FindChildrenByKeywords<T>(this Transform self, List<Transform> results,
            EnumSearchType searchType = EnumSearchType.Include, bool includeInactive = true, params string[] keywords) where T : Component
        {
            if (results == null) return;
            if (keywords == null || keywords.Length == 0)
            {
                Debug.LogWarning("FindChildrenByKeywords: No keywords provided.");
                return;
            }

            // 獲取暫存 List 與 用於去重的 HashSet (皆為 0 GC Alloc)
            List<T> compList = ListPool<T>.Get();
            HashSet<Transform> seenTransforms = HashSetPool<Transform>.Get();

            self.GetComponentsInChildren(includeInactive, compList);

            int count = compList.Count;
            for (int i = 0; i < count; i++)
            {
                T comp = compList[i];
                if (comp == null) continue;

                Transform target = comp.transform;
                if (target == self) continue;

                // 【邏輯修正】只有在 includeInactive == false (不含隱藏物件) 時，才需要嚴格檢查啟用狀態
                if (!includeInactive)
                {
                    bool isEnabled = comp switch
                    {
                        Behaviour b => b.enabled,
                        Collider c => c.enabled,
                        Renderer r => true,
                        // Renderer r => r.enabled,
                        _ => true
                    };

                    // 如果元件被關閉，或是整個 GameObject 其實是隱藏的，就跳過
                    if (!isEnabled || !target.gameObject.activeInHierarchy) continue;
                }

                // 防止重複加入 (一個物件上有多個同類型元件時去重)
                if (!seenTransforms.Add(target)) continue;

                // 如果沒有提供關鍵字，就直接加入所有符合條件的物件
                if (keywords == null || keywords.Length == 0)
                {
                    results.Add(target);
                }
                // 關鍵字比對
                else if (IsMatch(target.name, searchType, keywords))
                {
                    results.Add(target);
                }
            }

            // 釋放物件池
            HashSetPool<Transform>.Release(seenTransforms);
            ListPool<T>.Release(compList);
        }

        private static bool IsMatch(string name, EnumSearchType searchType, string[] keywords)
        {
            bool containsAny = name.ContainKeyword(keywords);
            return searchType switch
            {
                EnumSearchType.Include => containsAny,
                EnumSearchType.Exclude => !containsAny,
                _ => false
            };
        }

        public enum EnumSearchType
        {
            Include, // 只要包含任一關鍵字即可
            Exclude  // 不得包含任何關鍵字
        }
        #endregion

        #region 從父祖物件中TryGetComponent

        /// <summary>
        /// 嘗試從「真正的父物件」開始往上取得指定型別的元件（不包含自己）。
        /// </summary>
        public static bool TryGetComponentInParent<T>(this Transform self, out T component, bool includeInactive = true)
            where T : Component
        {
            if (self.parent == null)
            {
                component = null;
                return false;
            }
            return TryGetComponentInParent(self.gameObject, out component, includeInactive);
        }

        /// <summary>
        /// 嘗試從「真正的父物件」開始往上取得指定型別的元件（不包含自己）。
        /// </summary>
        public static bool TryGetComponentInParent<T>(this GameObject self, out T component, bool includeInactive = true)
            where T : Component
        {
            // 從父物件的 Transform 開始呼叫 Unity 原生的 GetComponentInParent
            Transform parentTransform = self.transform.parent;

            if (parentTransform != null)
            {
                component = parentTransform.GetComponentInParent<T>(includeInactive);
                return component != null;
            }

            component = null;
            return false;
        }

        #endregion

        #region 從子孫物件中TryGetComponent
        /// <summary>
        /// 嘗試從子孫物件中取得指定型別的元件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="componentRoot"></param>
        /// <param name="component"></param>
        /// <param name="includeInactive"></param>
        /// <returns></returns>
        public static bool TryGetComponentInChildren<T>(this Component componentRoot, out T component, bool includeInactive = false)
            where T : Component =>
            TryGetComponentInChildren(componentRoot.gameObject, out component, includeInactive);
        /// 嘗試從子孫物件中取得指定型別的元件
        public static bool TryGetComponentInChildren<T>(this GameObject gameObject, out T component, bool includeInactive = false)
            where T : Component
        {
            component = gameObject.GetComponentInChildren<T>(includeInactive);
            return component != null;
        }
        #endregion

        /// <summary>
        /// 取得模型物件所有Mesh（包含靜態與骨骼動畫）結合起來的正中心點（世界座標）
        /// <para>+ 適用於模型的 Pivot 不在中心點</para>
        /// <para>+ 若無 Renderer，則回傳物件本身的座標</para>
        /// </summary>
        public static Vector3 GetModelBoundsCenter(this Transform target)
        {
            Bounds bounds = new Bounds();
            bool hasBoundsInitialized = false;

            // 1. 處理靜態 Mesh (使用 for 迴圈)
            var meshRenderers = target.GetComponentsInChildren<MeshRenderer>();
            int meshCount = meshRenderers.Length;
            for (int i = 0; i < meshCount; i++)
            {
                if (!hasBoundsInitialized)
                {
                    bounds = meshRenderers[i].bounds; // 初始化 bounds 為第一個 MeshRenderer 的 bounds
                    hasBoundsInitialized = true;
                }
                else
                {
                    bounds.Encapsulate(meshRenderers[i].bounds); // 將後續的 MeshRenderer 的 bounds 包含進總 bounds 中
                }
            }

            // 2. 處理動態 Skinned Mesh (使用 for 迴圈)
            var skinnedRenderers = target.GetComponentsInChildren<SkinnedMeshRenderer>();
            int skinnedCount = skinnedRenderers.Length;
            for (int i = 0; i < skinnedCount; i++)
            {
                if (!hasBoundsInitialized)
                {
                    bounds = skinnedRenderers[i].bounds;
                    hasBoundsInitialized = true;
                }
                else
                {
                    bounds.Encapsulate(skinnedRenderers[i].bounds);
                }
            }

            // 如果連一個 Renderer 都沒找到，直接回傳物件本身的座標
            return hasBoundsInitialized ? bounds.center : target.position;
        }

        /// <summary>
        /// 以模型的幾何包圍盒中心點來設置物件的位置
        /// <para>+ 適用於模型的 Pivot 不在中心點，但你想要以模型的幾何中心來定位物件的情況</para>
        /// </summary>
        public static void SetPositionByModelBoundsCenter(this Transform target, Vector3 newCenterPos)
        {
            Vector3 modelCenter = target.GetModelBoundsCenter();
            Vector3 offset = target.position - modelCenter;
            target.position = newCenterPos + offset;
        }

        /// <summary>
        /// 以模型的幾何包圍盒中心點來設置物件的旋轉
        /// <para>+ 適用於模型的 Pivot 不在中心點，但你想要以模型的幾何中心為自轉軸來旋轉物件的情況</para>
        /// </summary>
        public static void SetRotationByModelBoundsCenter(this Transform target, Quaternion newRotation)
        {
            // 1. 取得旋轉前的幾何中心點（世界座標）
            Vector3 center = target.GetModelBoundsCenter();

            // 2. 計算「目前的軸心」相對於「中心點」的世界座標位移
            Vector3 worldOffset = target.position - center;

            // 3. 把這個世界座標位移，轉換成「不受目前旋轉影響」的區域座標位移（Local Offset）
            // 這樣不論物件本來轉成什麼角度，都能拿到純粹的相對方向與距離
            Vector3 localOffset = Quaternion.Inverse(target.rotation) * worldOffset;

            // 4. 套用新的旋轉
            target.rotation = newRotation;

            // 5. 以原本的中心點為基底，加上「新旋轉後的區域位移」，反推出新的 Pivot 位置
            target.position = center + (newRotation * localOffset);
        }


        #region MeshRenderer.material處理

        // 紀錄原本材質
        private static readonly Dictionary<Transform, Material[]> OriginalMaterials = new();

        /// [Extension] - 替換MeshRender.material為指定材質，並儲存原材質與Extension裡
        public static Transform ChangeMeshMaterialAndRecord(this Transform self, Material newMaterial, bool isIncludeChildren = true)
        {
            if (self.TryGetComponent(out MeshRenderer renderer))
            {
                // 若還沒記錄過才存
                if (!OriginalMaterials.ContainsKey(self)) OriginalMaterials[self] = renderer.materials;

                // 替換為指定材質
                Material[] newMaterials = new Material[renderer.materials.Length];
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    newMaterials[i] = newMaterial;
                }
                renderer.materials = newMaterials;
            }
            else Debug.LogWarning($"{self.name} has no MeshRenderer component");
            return self;
        }
        /// [Extension] - 還原先前記錄的材質
        public static Transform RestoreMeshMaterial(this Transform self)
        {
            if (OriginalMaterials.TryGetValue(self, out Material[] materialsResult))
            {
                if (self.TryGetComponent(out MeshRenderer renderer))
                {
                    renderer.materials = materialsResult;
                    OriginalMaterials.Remove(self);
                }
            }
            else Debug.LogWarning($"{self.name} has no recorded original materials to restore.");
            return self;
        }
        #endregion        
    }


    public enum EnumEaseType
    {
        Linear, EaseInQuad, EaseOutQuad, EaseInOutQuad, EaseInCubic, EaseOutCubic, EaseInOutCubic
    }

    public static class EasingResolver
    {
        public static Func<float, float> GetEase(EnumEaseType type)
        {
            return type switch
            {
                EnumEaseType.Linear => EaseForLerp.Linear,
                EnumEaseType.EaseInQuad => EaseForLerp.EaseInQuad,
                EnumEaseType.EaseOutQuad => EaseForLerp.EaseOutQuad,
                EnumEaseType.EaseInOutQuad => EaseForLerp.EaseInOutQuad,
                EnumEaseType.EaseInCubic => EaseForLerp.EaseInCubic,
                EnumEaseType.EaseOutCubic => EaseForLerp.EaseOutCubic,
                EnumEaseType.EaseInOutCubic => EaseForLerp.EaseInOutCubic,
                _ => EaseForLerp.Linear
            };
        }
    }

    public static class EaseForLerp
    {
        public static float Linear(float t) => t;

        public static float EaseInQuad(float t) => t * t;

        public static float EaseOutQuad(float t) => 1 - (1 - t) * (1 - t);

        public static float EaseInOutQuad(float t) =>
            t < 0.5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;

        public static float EaseInCubic(float t) => t * t * t;

        public static float EaseOutCubic(float t) => 1 - Mathf.Pow(1 - t, 3);

        public static float EaseInOutCubic(float t) =>
            t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;

        public static float EaseInSine(float t) => 1 - Mathf.Cos((t * Mathf.PI) / 2);

        public static float EaseOutSine(float t) => Mathf.Sin((t * Mathf.PI) / 2);

        public static float EaseInOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1) / 2;

        public static float EaseInExpo(float t) => t == 0 ? 0 : Mathf.Pow(2, 10 * (t - 1));

        public static float EaseOutExpo(float t) => t == 1 ? 1 : 1 - Mathf.Pow(2, -10 * t);

        public static float EaseInOutExpo(float t) =>
            t == 0 ? 0 : t == 1 ? 1 :
            t < 0.5f ? Mathf.Pow(2, 20 * t - 10) / 2 :
            (2 - Mathf.Pow(2, -20 * t + 10)) / 2;

        public static float EaseInBack(float t, float s = 1.70158f) =>
            s * t * t * ((s + 1) * t - s);

        public static float EaseOutBack(float t, float s = 1.70158f)
        {
            t -= 1;
            return 1 + s * t * t * ((s + 1) * t + s);
        }

        public static float EaseInOutBack(float t, float s = 1.70158f * 1.525f) =>
            t < 0.5f
                ? (Mathf.Pow(2 * t, 2) * ((s + 1) * 2 * t - s)) / 2
                : (Mathf.Pow(2 * t - 2, 2) * ((s + 1) * (t * 2 - 2) + s) + 2) / 2;
    }

}