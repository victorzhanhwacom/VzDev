/**
 * Unity WebGL <-> JavaScript 橋接工具 - 核心模組
 * 負責:Unity instance 註冊、JS -> Unity 送出邏輯、未就緒訊息暫存。
 * 此檔案獨立於 index.html 產物,位於 Assets/WebGLTemplates/ 下受版控管理,
 * 不會因 Unity 重新 Build 而被覆蓋。
 *
 * 接收 Unity 端訊息的函式(C# -> JS)定義在 unity-bridge-handlers.js,
 * 此檔案只負責「框架」本身,不包含任何業務邏輯。
 */

// 先確保 window.UnityBridge 存在,避免載入順序造成 handlers 檔案的內容被覆蓋
window.UnityBridge = window.UnityBridge || {};

window.unityReady = false;
window.unityInstance = null;

var _pendingMessages = [];

/**
 * Unity 初始化完成後,由 index.html 的 .then() 呼叫此函式註冊 instance。
 */
function registerUnityInstance(instance) {
  window.unityInstance = instance;
  window.unityReady = true;
  console.log("[UnityBridge] Unity 初始化完成");

  flushPendingMessages();
}

function flushPendingMessages() {
  if (_pendingMessages.length === 0) return;
  var queued = _pendingMessages;
  _pendingMessages = [];
  queued.forEach(function (msg) {
    sendToUnityByCustom(msg.gameObjectName, msg.methodName, msg.payload);
  });
}

// 送出訊息給 Unity端 ===============================

/**
 * sendToUnity(數字string);
 * sendToUnityByCustom("WebGLBridge", "OnReceiveFromJS", 數字string);
 * [切換主選單的頁籤]
 * 0: 能源管理
 * 1: 環境管理
 * 2: CCTV
 * 3: 門禁
 * 4: BMS
 * 5: ICT
 * 6: 配置管理
 * 7: 告警管理
 */

/** 【JS to Unity 送出訊息的範例】
 * sendToUnityByCustom("WebGLBridge_User", "SetUserToken", Token string);         //使用者 Token
 * sendToUnityByCustom("WebGLBridge_MainMenu", "SetMainMenu", 數字string);        //主選單索引
 * sendToUnityByCustom("WebGLBridge_Env", "SetSubMenu", 數字string);              //環控子選單索引
 * 
 * 攝影機焦點切換 ///
 * sendToUnityByCustom("WebGLBridge_Camera", "SetCameraFocus_Floor", 樓層字串);  //攝影機焦點切換_樓層：All / RF / 15F / B1F
 * sendToUnityByCustom("WebGLBridge_Camera", "SetCameraFocus_CCTV", deviceCode);  //攝影機焦點切換_CCTV
 * sendToUnityByCustom("WebGLBridge_Camera", "SetCameraFocus_Door", deviceCode);  //攝影機焦點切換_門禁
 * 
 * 模型點位標籤Toggle切換 ///
 * sendToUnityByCustom("WebGLBridge_ModelToggleTag", "SetCctvToggleOn", deviceCode);
 * sendToUnityByCustom("WebGLBridge_ModelToggleTag", "SetCctvToggleOff", deviceCode);
 * sendToUnityByCustom("WebGLBridge_ModelToggleTag", "SetDoorToggleOn", deviceCode);
 * sendToUnityByCustom("WebGLBridge_ModelToggleTag", "SetDoorToggleOff", deviceCode);
 * sendToUnityByCustom("WebGLBridge_ModelToggleTag", "SetAcSystemToggleOn", deviceCode);
 * sendToUnityByCustom("WebGLBridge_ModelToggleTag", "SetAcSystemToggleOff", deviceCode);
 */

const UnityObjName = "WebGLBridge";
const UnityMethodName = "ReceiveFromJS";

/**
 * 送出訊息給預設的 Unity 物件/方法 (UnityObjName / UnityMethodName)。
 * @param {object|string} payload 任意物件(會自動 JSON.stringify)或字串
 */
function SendToUnity(payload) {
  SendToUnityByCustom(UnityObjName, UnityMethodName, payload);
}

/**
 * 送出訊息給指定的 Unity 物件/方法。
 * @param {string} gameObjectName 場景上接收訊息的 GameObject 名稱
 * @param {string} methodName 該物件上要呼叫的 public 方法
 * @param {object|string} payload 任意物件(會自動 JSON.stringify)或字串
 */
function SendToUnityByCustom(gameObjectName, methodName, payload) {
  if (typeof gameObjectName !== "string" || typeof methodName !== "string") {
    console.error("[UnityBridge] gameObjectName / methodName 必須是字串,實際收到:", gameObjectName, methodName);
    return;
  }

  if (!window.unityReady || !window.unityInstance) {
    console.warn("[UnityBridge] Unity 尚未就緒,訊息已暫存,待就緒後補送");
    _pendingMessages.push({ gameObjectName, methodName, payload });
    return;
  }

  var data = typeof payload === "string" ? payload : JSON.stringify(payload);

  try {
    window.unityInstance.SendMessage(gameObjectName, methodName, data);
    console.log("[UnityBridge] SendMessage To Unity -> " + gameObjectName + "." + methodName, data);
  } catch (e) {
    console.error("[UnityBridge] SendMessage 失敗:", e);
  }
}
// 送出訊息給 Unity端 ===============================
