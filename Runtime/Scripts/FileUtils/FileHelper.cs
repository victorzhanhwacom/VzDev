using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using VzDev.DateTimeUtils;
using JetBrains.Annotations;
//using SFB;
using UnityEditor;
using UnityEngine;
using Debug = VzDev.ToolUtils.Debug;
using VzDev.NetUtils;

namespace VzDev.FileUtils
{
    public static class FileHelper
    {
        /// <summary>
        /// 直接讀取文字檔案內容，不考慮讀取時間
        /// </summary>
        public static string LoadTextFileDirectly(string filePath, EnumFilePath enumFilePath = EnumFilePath.streamingAssetsPath)
        {
            string fullPath = GetFullPath(filePath, enumFilePath);
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"找不到檔案: {fullPath}");
                return null;
            }

            string jsonContent = File.ReadAllText(fullPath);
            Debug.Log($"讀取成功:\n{jsonContent}");
            return jsonContent;
        }

        /// <summary>
        /// 取得檔案完整路徑
        /// </summary>
        public static string GetFullPath(string filePath, EnumFilePath enumFilePath)
        {
            string basePath = enumFilePath switch
            {
                EnumFilePath.streamingAssetsPath => Application.streamingAssetsPath,
                EnumFilePath.persistentDataPath => Application.persistentDataPath,
                EnumFilePath.dataPath => Application.dataPath,
                _ => throw new ArgumentOutOfRangeException(nameof(enumFilePath), enumFilePath, null)
            };
            return Path.Combine(basePath, filePath);
        }

        ///////// 20260824 /////////

        #region 檔案產生與存儲

        private static string DefaultExportFolder => "DefaultExportFolder";

        /// [Extended] - 存儲String為實體檔案
        public static async Task<string> SaveTextAsync(string text, string fileName, string folder = "")
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            fileName = fileName.Trim();
            if (IsTextFile(fileName) == false) fileName += ".txt";
            return await SaveBytesAsync(bytes, fileName, folder);
        }

        /// [Extended] - 存儲Texture2D為實體檔案
        public static async Task<string> SaveTexturePNGAsync(Texture2D tex, string fileName, string folder = "")
        {
            byte[] bytes = tex.EncodeToPNG(); // ← 必須在主執行緒
            fileName = fileName.Trim();
            if (IsImageFile(fileName) == false) fileName += ".png";
            return await SaveBytesAsync(bytes, fileName, folder);
        }

        /// 存任意資料（圖片、二進位、設定檔…），並回傳路徑
        public static async Task<string> SaveBytesAsync(byte[] bytes, string fileName, string folder = "")
        {
            if (string.IsNullOrEmpty(folder)) folder = DefaultExportFolder;
            string folderPath = GetFolderPath(folder);
            string filePath = Path.Combine(folderPath, fileName);

            // 非主執行緒，不卡
            await Task.Run(() => { File.WriteAllBytes(filePath, bytes); });

#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
            return filePath;
        }


        // 建立資料夾，並回傳路徑
        public static string GetFolderPath(string folder = "")
        {
            folder = folder.Trim();
            if (string.IsNullOrEmpty(folder)) folder = DefaultExportFolder;
            string root = Application.dataPath;
            string folderPath = Path.Combine(root, folder);

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            return folderPath;
        }

        /// 檔名是否為文字檔
        public static bool IsTextFile(string fileName)
        {
            fileName = fileName.Trim();
            if (string.IsNullOrEmpty(fileName)) return false;
            string[] textExtensions = { ".txt" };
            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            return Array.Exists(textExtensions, x => x == ext);
        }

        /// 檔名是否為圖像檔
        public static bool IsImageFile(string fileName)
        {
            fileName = fileName.Trim();
            if (string.IsNullOrEmpty(fileName)) return false;
            string[] imageExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tga" };
            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            return Array.Exists(imageExtensions, x => x == ext);
        }

        #endregion


        /// 彈跳出下載路徑選擇視窗 (PC版本)
        /// <para>+ 需安裝UnityStandaloneFileBrowser </para>
        /// <para>+ https://github.com/gkngkc/UnityStandaloneFileBrowser?tab=readme-ov-file </para>
        /// <returns>檔案路徑/檔名.副檔名</returns>
        /*public static async Task<string> SaveFileWithPopupWindow(byte[] fileBytes, string defaultFileName = null)
        {
            var extension = new[]
            {
                new ExtensionFilter("Excel files", "xlsx")
            };
            defaultFileName ??= GetDefaultFileName();
            string filePath = string.Empty;

            // 彈出儲存檔案視窗
#if UNITY_STANDALONE_WIN || UNITY_WEBGL
            filePath = StandaloneFileBrowser.SaveFilePanel("Save Excel File", "", defaultFileName, extension);
#else
            filePath = GetDownloadFilePath();
#endif
            if (!string.IsNullOrEmpty(filePath))
            {
                await File.WriteAllBytesAsync(filePath, fileBytes);
                OpenFileOrFolder(filePath);
            }
            else Debug.Log("User cancelled save dialog");

            return filePath;
        }*/

        /// 依平台類型回傳下載檔案路徑
        public static string GetDownloadFilePath() => Application.platform == RuntimePlatform.Android
            ? Application.persistentDataPath
            : Path.Combine(Application.dataPath, "StreamingAssets");

        /// 取得預設檔案名稱
        public static string GetDefaultFileName() => $"downloadFile_{DateTime.Now:yyyyMMddHHmmss}";

        /// 依BLOB資料生成檔案
        /// <para>return string[]: [資料夾路徑][檔案名稱(包含副檔名)]</para>
        public static string[] SaveToStreamingAssetsFolder(byte[] fileData, [CanBeNull] string fileFullName = null,
            bool isAutoOpen = true)
        {
            string folderPath = GetDownloadFilePath();
            // 若資料夾不存在，建立它
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            fileFullName ??= $"DownloadFile-{DateTime.Today.ToString(DateTimeHelper.FullDateFormat)}";
            string filePath = Path.Combine(folderPath, fileFullName);
            File.WriteAllBytes(filePath, fileData);
            Debug.Log($"檔案已儲存至:{filePath}");

            if (isAutoOpen)
            {
                try
                {
                    // 開啟 Excel 檔案
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true // 讓作業系統選擇合適的應用程式來開啟
                    });
                }
                catch (Exception e)
                {
                    Debug.LogError($"開啟{fileFullName}時發生錯誤: " + e.Message);
                }
            }

            return new[] { folderPath, fileFullName };
        }

        /// 從HttpContent Header裡取得回傳的資料類型
        public static EnumResponseDataType GetResponseDataTypeFromHttpHeader(HttpContent content)
        {
            string contentType = content.Headers.ContentType?.MediaType;
            if (string.IsNullOrEmpty(contentType)) return EnumResponseDataType.Binary;
            if (ContentTypeToEnumMap.TryGetValue(contentType, out EnumResponseDataType dataType))
                return dataType;
            // fallback
            if (contentType.StartsWith("image/"))
                return EnumResponseDataType.Image;

            if (contentType.StartsWith("text/"))
                return EnumResponseDataType.Text;
            return EnumResponseDataType.Binary;
        }

        /// 從HttpContent Header裡取得檔名.副檔名
        public static string GetFileNameFromHttpHeader(HttpContent content)
        {
            string rawFileName = content.Headers.ContentDisposition.FileName;
            if (string.IsNullOrEmpty(rawFileName)) return string.Empty;
            string fileName = rawFileName.Trim('\"'); // 去掉前後引號（有些會包雙引號）
            return fileName.Replace("/", "").Replace(":", "").Replace(" ", "");
        }

        private static readonly Dictionary<string, EnumResponseDataType> ContentTypeToEnumMap = new()
        {
            { "application/json", EnumResponseDataType.Json },
            { "application/x-www-form-urlencoded", EnumResponseDataType.WWWForm },
            { "text/plain", EnumResponseDataType.Text },
            { "text/html", EnumResponseDataType.Text },

            // Excel
            { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", EnumResponseDataType.Excel },
            { "application/vnd.ms-excel", EnumResponseDataType.Excel },

            // PDF
            { "application/pdf", EnumResponseDataType.PDF },

            // 圖片
            { "image/png", EnumResponseDataType.Image },
            { "image/jpeg", EnumResponseDataType.Image },
            { "image/gif", EnumResponseDataType.Image },

            // Word
            { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", EnumResponseDataType.Word },

            // ZIP
            { "application/zip", EnumResponseDataType.ZIP },

            // 萬用二進位檔案
            { "application/octet-stream", EnumResponseDataType.Binary },
        };

        /// 開啟檔案 / 資料夾 (Windows)
        public static void OpenFileOrFolder(string filePath)
        {
#if UNITY_STANDALONE_WIN
            try
            {
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to open file: {ex.Message}");
            }
#elif UNITY_WEBGL
            Application.OpenURL(filePath);
#endif
        }

        /// 將content寫入文字檔內，寫入動作可以不管檔案是否已存在
        public static void WriteStringToTextFile(string filePath, string content)
        {
            filePath = filePath.Trim();
            content = content.Trim(' ');
            try
            {
                File.AppendAllText(filePath, content);
            }
            catch (Exception e)
            {
                // 若寫檔失敗，別再用 File.AppendAllText (避免遞迴)；改用 Unity 的 console 提示
                Debug.LogWarning($"寫入失敗: {e.Message}");
            }
        }
    }
}