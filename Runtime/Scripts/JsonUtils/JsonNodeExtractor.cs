using NaughtyAttributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;
using Debug = VzDev.Extensions.Debug;

namespace VzDev.JsonUtils
{
    /// <summary>
    /// JsonNodeExtractor 是一個 Unity 組件，用於從 JSON 字串中提取指定節點的內容。
    /// 使用者可以在 Inspector 中設定節點路徑（使用點號分隔）和輸出格式化選項，並透過事件回調獲取提取結果。
    /// </summary>
    public class JsonNodeExtractor : MonoBehaviour
    {
        #region Fields
        [Foldout("[Events]")] public UnityEvent<string> onExtracted;
        [Foldout("[Settings]"), SerializeField] private string nodePath = "vendors";
        [Foldout("[Settings]"), SerializeField] private Formatting formatting = Formatting.None;

        private string[] _pathSegments;
        private string _cachedNodePath;

        #endregion

        private void Awake() => EnsurePathSegments();
        private void EnsurePathSegments()
        {
            if (_pathSegments != null && _cachedNodePath == nodePath) return;
            if (string.IsNullOrEmpty(nodePath))
            {
                Debug.LogWarning("[JsonNodeExtractor] Node path is empty. Please set a valid node path.");
            }
            _pathSegments = nodePath.Split('.');
            _cachedNodePath = nodePath;
        }

        /// <summary>
        /// 從給定的 JSON 字串中提取指定節點的內容，並透過事件回調返回結果。
        /// </summary>
        public void Extract(string jsonString)
        {
            EnsurePathSegments();

            if (string.IsNullOrEmpty(jsonString) || string.IsNullOrEmpty(nodePath))
            {
                Debug.LogWarning("[JsonNodeExtractor] Invalid input: JSON string or node path is empty.");
                onExtracted?.Invoke(jsonString); // 如果輸入無效，直接回傳原始字串
                return;
            }

            JToken node;
            try
            {
                node = JToken.Parse(jsonString);
            }
            catch (JsonReaderException ex)
            {
                Debug.LogError($"[JsonNodeExtractor] Parse failed: {ex.Message}");
                return;
            }

            foreach (string key in _pathSegments)
            {
                node = node?[key];
                if (node == null) break; // 提早跳出，避免無意義的後續索引
            }
            string result = node == null ? ""
            : node.Type is JTokenType.Object or JTokenType.Array
                ? node.ToString(formatting)
                : node.ToString();

            onExtracted?.Invoke(result);

            if (node == null)
                Debug.LogWarning($"[JsonNodeExtractor] Node not found at path: {nodePath}");
        }
    }
}