using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using VzDev.MediatorUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Debug = VzDev.ToolUtils.Debug;

namespace VzDev.DebugUtils
{
    /// GameObject物件處理
    public static class ObjectHelper
    {
  /// 依照環境 Runtime/Editor 來實例化對像物件T
        /// <para>+ 若container為null，則實例化在Scene根節點</para>
        public static T Instantiate<T>(T prefab, Transform container = null) where T : Component
        {
            T result = null;
#if UNITY_EDITOR
            // 檢查是否為真正的 Prefab 資產
            if (IsPrefabAsset(prefab))
            {
                // 1. 傳入 prefab.gameObject，確保生成整個物件
                // 2. 將回傳的 Object 強轉為 GameObject，才能呼叫 GetComponent<T>()
                GameObject instantiatedObj = PrefabUtility.InstantiatePrefab(prefab.gameObject, container) as GameObject;
                if (instantiatedObj != null)
                {
                    result = instantiatedObj.GetComponent<T>();
                }
            }
            else
            {
                result = Object.Instantiate(prefab, container);
            }
#else
            // 運行時使用普通的 Instantiate
            result = Object.Instantiate(prefab, container);
#endif
            return result;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 檢查該組件所屬的 GameObject 是否為 Prefab 資產（非場景中的實例）
        /// </summary>
        public static bool IsPrefabAsset<T>(T target) where T : Component
        {
            if (target == null) return false;
            // 關鍵：必須轉成 gameObject 判定，否則傳入 Component 可能會誤判為 NotAPrefab
            GameObject targetGO = target.gameObject;
            // 檢查是否為 Prefab 資產（Model、Regular、Variant 等皆算）
            PrefabAssetType assetType = PrefabUtility.GetPrefabAssetType(targetGO);
            // 檢查是否為場景中已擺放的實例（若是場景中的 Prefab 實例，我們不該用 InstantiatePrefab）
            PrefabInstanceStatus instanceStatus = PrefabUtility.GetPrefabInstanceStatus(targetGO);
            return assetType != PrefabAssetType.NotAPrefab && instanceStatus == PrefabInstanceStatus.NotAPrefab;
        }
#endif

        /// <summary>
        /// 刪除目標物件，依照環境Runtime/Editor來決定使用Destroy或DestroyImmediate
        /// </summary>
        public static void Destroy<T>(T target) where T : Object
        {
            if (target == null) return;
            Object finalTarget = target;
            if (target is GameObject go)
                finalTarget = go;
            else if (target is RectTransform rectTransform)
                finalTarget = rectTransform.gameObject;
            if (Application.isPlaying) Object.Destroy(finalTarget);
            else Object.DestroyImmediate(finalTarget);
        }


///////////////////////////////////////////////////////// Refactorying //////////////////////////////////////////////////////////

        #region 依關鍵字尋找物件
        /// 根據關鍵字，針對目標物件底下所有子物件進行比對，找出名字包含關鍵字的子物件
        public static List<Transform> FindObjectsByKeywords(Transform target, List<string> keywords
            , bool isExceptKeywords = false, bool isNeedMeshRenderer = true, bool isCaseSensitive = false)
        {
            void FindObjectsRecursively(Transform parent, List<Transform> result)
            {
                foreach (Transform child in parent)
                {
                    // if(isNeedMeshRenderer && child.TryGetComponent(out MeshRenderer meshRenderer) == false) continue;
                    // 檢查名稱是否包含任意關鍵字
                    foreach (string keyword in keywords)
                    {
                        string childName = (isCaseSensitive) ? child.name : child.name.ToLower();
                        string key = (isCaseSensitive) ? keyword : keyword.ToLower();

                        if (childName.Contains(key) == !isExceptKeywords)
                        {
                            result.Add(child);
                            break; // 如果符合任意一個關鍵字，跳出內層迴圈
                        }
                    }

                    // 遞歸檢查子物件
                    FindObjectsRecursively(child, result);
                }
            }

            List<Transform> matchingObjects = new List<Transform>();
            FindObjectsRecursively(target, matchingObjects);
            return matchingObjects;
        }
        #endregion


        /// 容器下實作TComponent的對像依照Name進行排序
        public static void SortTargetsByObjectName<TComponent>(Transform container, bool isDescending = false) where TComponent : Component
        {
            // 取得所有子物件中有 ClassA 的項目
            List<Transform> itemList = container.GetComponentsInChildren<TComponent>(includeInactive: false)
                .Select(item => item.transform).ToList();

            // 排序
            if (isDescending)
                itemList = itemList.OrderByDescending(target => target.name, System.StringComparer.OrdinalIgnoreCase).ToList();
            else
                itemList = itemList.OrderBy(t => t.name, System.StringComparer.OrdinalIgnoreCase).ToList();

            // 根據排序結果重新設定 Hierarchy 順序
            for (int i = 0; i < itemList.Count; i++)
                itemList[i].SetSiblingIndex(i);
        }

        /// 取得類別裡的所有變數名稱
        public static List<string> GetFieldNames<T>(params string[] skipFiledName)
        {
            var type = typeof(T);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var list = new List<string>();
            foreach (var field in fields)
            {
                if (skipFiledName.Contains(field.Name)) continue;
                list.Add(field.Name);
            }
            return list;
        }

        /// 依字串取得目標類別實例的參數值
        public static string GetValueByFiledName<T>(T target, string fieldName)
        {
            fieldName = fieldName.Trim();
            // 取得欄位值
            FieldInfo fieldInfo = typeof(T).GetField(fieldName);
            if (fieldInfo != null)
            {
                object fieldValue = fieldInfo.GetValue(target);
                return fieldValue?.ToString();
            }
            return null;
        }

        /// 設定Target的Scale大小，等於reference的meshRenderer大小
        public static void SetMatchSizeAndPosition(Transform target, Transform reference, float adjustScale = 1.01f)
        {
            Renderer targetRenderer;
            if (target.TryGetComponent(out targetRenderer) == false)
            {
                Debug.LogWarning($"{targetRenderer.name} 沒有 Renderer");
                return;
            }

            Renderer refRenderer;
            if (reference.TryGetComponent(out refRenderer) == false)
            {
                Debug.LogWarning($"{reference.name} 沒有 Renderer");
                return;
            }

            // 取得 reference 的 bounds 尺寸
            Vector3 refSize = refRenderer.bounds.size;
            Vector3 targetSize = targetRenderer.bounds.size;

            if (targetSize == Vector3.zero)
            {
                Debug.LogWarning("Target 的 Renderer size 是 0，可能是模型沒載入或尺寸為零");
                return;
            }

            // 計算比例並應用到 localScale
            Vector3 scaleRatio = new Vector3(
                refSize.x / targetSize.x,
                refSize.y / targetSize.y,
                refSize.z / targetSize.z
            );

            // 根據目前的 localScale 進行縮放
            target.localScale = Vector3.Scale(target.localScale, scaleRatio) * adjustScale;
            target.position = refRenderer.bounds.center;
        }


        /// 從子物件起開始尋找有實作T Class的Monobehaviour
        /// 通常會用於尋找實作Interface的對像
        public static List<MonoBehaviour> FindChildrenByClass<TFind>(Transform parent, bool includeInactive = true, params string[] keywords)
            where TFind : Component
            => FindChildrenByClass<TFind, MonoBehaviour>(parent, includeInactive, keywords);

        public static List<TResult> FindChildrenByClass<TFind, TResult>(Transform parent, bool includeInactive = true, params string[] keywords)
            where TFind : Component where TResult : class
        {
            TFind[] components = parent.GetComponentsInChildren<TFind>(includeInactive);
            List<TResult> results = new List<TResult>(components.Length); // 預先分配容量

            // ⚡ 提前轉成 HashSet 提高查找效率
            HashSet<string> keywordSet = keywords is { Length: > 0 } ? new HashSet<string>(keywords) : null;

            foreach (var target in components)
            {
                // 如果目標不為TResult類型
                if (target is not TResult result) continue;
                // 如果有設定關鍵字，就做比對
                if (keywordSet != null && keywordSet.Contains(target.gameObject.name) == false) continue;
                results.Add(result);
            }
            return results;
        }

      


        /// 檢查目標物件是否為Null
        public static bool CheckTargetsIsNull(params object[] targets)
        {
            bool result = false;
            foreach (var target in targets)
            {
                if (target is Object unityObj && unityObj == null)
                {
                    result = true;
                }
                else if (target == null)
                {
                    result = true;
                }
            }
            return result;
        }

        /// 刪除目標容器下的所有物件
        public static void DestoryObjectsOfContainer(Transform container)
        {
            //先複製一份List，進行刪除子物件時才不會影響到Fore迴圈
            List<Transform> objectList = container.Cast<Transform>().ToList();
            objectList.ForEach(Destroy);
            objectList.Clear();
        }



        /// 將子物件前後排序顛倒
        public static void ReverseChildrenOrder(Transform parent)
        {
            List<Transform> children = parent.GetComponentsInChildren<Transform>().ToList();
            children.ForEach(child => child.SetAsFirstSibling());
        }

        /// ==================================================================================




        /// [泛型] 新增Collider型別到目標物件上
        /// <para>+ 泛型Collider只需傳入隨意一個實例化即可，例如: new BoxCollider()</para>
        public static void AddColliderToObjects<T>(List<Transform> targetObjects,
            bool removeExisting = true) where T : Collider
        {
            if (removeExisting) RemoveColliderFromObjects(targetObjects);
            targetObjects.ForEach(obj => obj.gameObject.AddComponent<T>());
        }


        /// [泛型] 移除Collider型別到目標物件上
        public static void RemoveColliderFromObjects(List<Transform> targetObjects)
            => targetObjects.ForEach(obj =>
            {
                Collider[] existingColliders = obj.GetComponents<Collider>();
                foreach (var collider in existingColliders)
                {
#if UNITY_EDITOR
                    Object.DestroyImmediate(collider);
#else
                   Object.Destroy(collider);
#endif
                }
            });


        /// 檢查B的包圍盒是否完全位於A的包圍盒內
        public static bool IsModelBCompletelyInsideModelA(Collider modelA, Collider modelB)
            => modelA.bounds.Contains(modelB.bounds.min) && modelA.bounds.Contains(modelB.bounds.max);


        /// 檢查B是否部分在A內
        public static bool IsModelBPartiallyInsideModelA(Collider modelA, Collider modelB)
            => modelA.bounds.Intersects(modelB.bounds);


        /// 將底下所有子物件，依照Y軸高底進行排序
        public static void SortingChildByPosY(Transform target)
        {
            // 將子物件依 Y 座標排序（從高到低）
            var sortedChildren = target.Cast<Transform>()
                .OrderByDescending(child => child.position.y)
                .ToList();
            // 更新 Sibling Index
            for (int i = 0; i < sortedChildren.Count; i++)
            {
                sortedChildren[i].SetSiblingIndex(i);
            }
        }
        /// 檢查List裡面是否有實作T類別，將不符合的從List裡移除
        public static List<MonoBehaviour> CheckTypesOfList(List<MonoBehaviour> list, BoolLogicGate.BoolGateType logic, params Type[] types)
        {
            if (list == null) return null;

            StackTrace stackTrace = new StackTrace();
            StackFrame frame = stackTrace.GetFrame(1);
            var method = frame.GetMethod();

            for (int i = list.Count - 1; i >= 0; i--)
            {
                MonoBehaviour target = list[i];
                if (target == null)
                {
                    list.RemoveAt(i);
                    continue;
                }

                bool isValid = logic switch
                {
                    BoolLogicGate.BoolGateType.AND => types.All(t => t.IsAssignableFrom(target.GetType())),
                    BoolLogicGate.BoolGateType.OR => types.Any(t => t.IsAssignableFrom(target.GetType())),
                    _ => false
                };

                if (!isValid)
                {
                    Debug.Log(
                        $">>> [接收器：{method.DeclaringType?.Name}] - 物件：{{{target.name}}} 未符合 {logic} 條件, 已移除.");
                    list.RemoveAt(i);
                }
            }

            return list;
        }

        /// 檢查List裡面是否有實作T類別，將不符合的從List裡移除
        public static List<MonoBehaviour> CheckTypeOfList<T>(List<MonoBehaviour> list) where T : class
        {
            if (list == null) return null;

            #region 取得上一層呼叫的資訊

            StackTrace stackTrace = new StackTrace();
            StackFrame frame = stackTrace.GetFrame(1);
            var method = frame.GetMethod();

            #endregion

            for (int i = 0; i < list.Count; i++)
            {
                MonoBehaviour target = list[i];
                if (target != null && target is not T item)
                {
                    Debug.Log(
                        $">>> [接收器：{method.DeclaringType?.Name}] - 物件：{{{target.name}}} 並沒有實作{typeof(T).Name}, 已從列表移除.");
                    list.Remove(target);
                }
            }
            return list;
        }

        /// 檢查List裡面是否有繼承T類別，將不符合的從List裡移除
        public static List<MonoBehaviour> CheckSubTypoOfList<T>(List<MonoBehaviour> list) where T : class
        {
            #region 取得上一層呼叫的資訊

            StackTrace stackTrace = new StackTrace();
            StackFrame frame = stackTrace.GetFrame(1);
            var method = frame.GetMethod();

            #endregion

            for (int i = 0; i < list.Count; i++)
            {
                MonoBehaviour target = list[i];
                if (target != null && target.GetType().IsSubclassOf(typeof(T)) == false)
                {
                    Debug.Log(
                        $">>> [接收器：{method.DeclaringType?.Name}] - 物件：{{{target.name}}} 並非繼承{typeof(T).Name}, 已從列表移除.");
                    list.Remove(target);
                }
            }

            return list;
        }

        /// 取得場景上所有包含T類別的物件(包括inactive)
        public static List<T> FindAllObjectOfType<T>() where T : class
        {
            IEnumerable<GameObject> GetAllChildren(GameObject parent)
            {
                yield return parent;
                foreach (Transform child in parent.transform)
                {
                    foreach (var descendant in GetAllChildren(child.gameObject))
                    {
                        yield return descendant;
                    }
                }
            }

            List<T> result = SceneManager.GetActiveScene()
                .GetRootGameObjects() // 取得所有根物件
                .SelectMany(GetAllChildren).Where(obj => obj.GetComponent<T>() != null)
                .Select(target => target.GetComponent<T>()).ToList(); // 遍歷所有子物件
            return result;
        }

        /// 檢查與Target模型相交之模型
        public static List<GameObject> GetModelInBound(GameObject target, List<GameObject> models,
            List<string> keywordExclude = null, List<string> keywordInclude = null)
        {
            Debug.Log($":> [ObjectHelper] - 檢查與{target.name}相交之模型... ");

            List<GameObject> result = null;
            if (target.TryGetComponent(out Collider targetCollider))
            {
                result = new List<GameObject>();

                Bounds objectBounds = targetCollider.bounds;
                Collider[] colliders = models.Select(model => model.GetComponent<Collider>()).ToArray(); // 找到所有Collider

                foreach (Collider col in colliders)
                {
                    //檢查Collider是否有交互
                    if (col.gameObject != target && objectBounds.Intersects(col.bounds))
                    {
                        result.Add(col.gameObject);
                    }
                }

                // 判斷排除關鍵字
                if (keywordExclude != null)
                {
                    result = result.Where(model =>
                        !keywordExclude.Any(keyword =>
                            model.name.Contains(keyword, StringComparison.OrdinalIgnoreCase))).ToList();
                }

                // 判斷包含關鍵字
                if (keywordInclude != null)
                {
                    result = result.Where(model =>
                            keywordInclude.Any(keyword =>
                                model.name.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                }
            }

            Debug.Log($":> [ObjectHelper] - 檢查與{target.name}相交之模型... Done.");
            return result;
        }

        /// 以Target模型尺吋，建立同尺吋的Cube模型
        public static GameObject CreateBoundingCube(GameObject target, Material material = null,
            float sizeOffset = 0.01f)
        {
            if (target.TryGetComponent(out Renderer renderer) == false)
            {
                Debug.LogError("[ObjectHelper] - target模型沒有 Renderer，無法獲取邊界");
                return null;
            }

            // 獲取模型的邊界尺寸
            Vector3 boundsSize = renderer.bounds.size;

            // 創建新的 Cube
            GameObject boundObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boundObj.transform.SetParent(target.transform, true);
            boundObj.name = "BoundObject";

            // 設置 Cube 的大小與模型一致
            boundObj.transform.localScale = boundsSize + new Vector3(sizeOffset, sizeOffset, sizeOffset);

            // 設置 Cube 的位置，使其中心與模型一致
            boundObj.transform.position = renderer.bounds.center;

            // 可選：更改顏色讓 Cube 容易識別
            if (material != null) boundObj.GetComponent<MeshRenderer>().material = material;

            return boundObj;
        }

    }
}