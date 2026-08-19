mergeInto(LibraryManager.library, {
  // C# -> JS
  Unity_SendToJS: function (functionNamePtr, payloadPtr) {
    var functionName = UTF8ToString(functionNamePtr);
    var payload = UTF8ToString(payloadPtr);

    if (window.UnityBridge && typeof window.UnityBridge[functionName] === "function") {
      window.UnityBridge[functionName](payload);
    } else {
      console.warn("[UnityBridge] 找不到對應的 JS 處理函式: " + functionName, payload);
    }
  }
});