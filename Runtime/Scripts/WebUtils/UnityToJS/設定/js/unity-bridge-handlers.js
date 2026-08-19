/**
 * Unity -> JavaScript 訊息接收處理清單
 * 對應 Unity C# 端呼叫: SendToJS(functionName, payload)
 * 會執行 window.UnityBridge[functionName](payload)
 *
 * 此檔案專門放「業務邏輯」,每個專案/模組要新增接收函式時,
 * 只需要在這裡加,不需要碰 unity-bridge-core.js。
 */

// 因為 core 檔案已經先建立過 window.UnityBridge,這裡用 Object.assign 疊加,
// 避免不管載入順序先後,都不會整包覆蓋掉。
Object.assign(window.UnityBridge, {
  //接收載入完成訊息
  OnUnityReady: function (isReady) {
    console.log("[OnUnityReady] isReady:", isReady);
  },
  
  //Unity傳給JS
  SendToJS: function (payload) {
    console.log("[SendToJs] payload:", payload);
  },
});
