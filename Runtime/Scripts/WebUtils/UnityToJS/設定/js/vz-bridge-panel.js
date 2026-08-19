// vz-bridge-panel.js
// Unity WebGL <-> JS 浮動溝通面板（全自動生成版）
// --------------------------------------------------
// 使用方式：
//   index.html 只需要加這一行，放在 unity-bridge-core.js（以及若有
//   unity-bridge-handlers.js）之後、Unity loader script 之後即可：
//     <script src="TemplateData/vz-bridge-panel.js"></script>
//
//   不需要另外貼 HTML、不需要另外連 CSS，
//   樣式與 DOM 結構都由這支腳本自己建立並插入頁面。
//
//   送出（JS -> Unity）：優先使用專案既有的 SendToUnityByCustom()
//   （unity-bridge-core.js 提供），沒有的話 fallback 直接呼叫
//   unityInstance.SendMessage()。
//
//   接收（Unity -> JS）：用 Proxy 掛勾 window.UnityBridge，攔截所有
//   handler 呼叫（unity-bridge-handlers.js 裡定義的 OnUnityReady、
//   SendToJS...等，包含未來新增的），不需要修改那兩個檔案。
// --------------------------------------------------

(function () {
  // ====== fallback 用，若專案沒有 unity-bridge-core.js 提供的
  // UnityObjName / UnityMethodName / SendToUnityByCustom 時才會用到這兩個 ======
  var UNITY_OBJECT_NAME = "WebGLBridge";
  var UNITY_METHOD_NAME = "ReceiveFromJS";

  // -------------------- 1. 注入樣式 --------------------
  var css = `
    #vz-json-panel {
      position: fixed;
      top: 20px;
      right: 20px;
      width: 340px;
      height: 320px;
      min-width: 260px;
      min-height: 180px;
      background: rgba(20, 20, 20, 0.92);
      border: 1px solid #444;
      border-radius: 8px;
      box-shadow: 0 4px 16px rgba(0, 0, 0, 0.5);
      font-family: -apple-system, "Segoe UI", sans-serif;
      color: #eee;
      z-index: 99999;
      display: flex;
      flex-direction: column;
      overflow: hidden;
      resize: both;
    }
    #vz-json-panel.vz-hidden { display: none; }
    #vz-json-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 8px 10px;
      background: #2b2b2b;
      cursor: move;
      font-size: 13px;
      font-weight: 600;
      border-bottom: 1px solid #444;
      user-select: none;
      flex-shrink: 0;
    }
    #vz-json-body {
      flex: 1;
      padding: 10px;
      display: flex;
      flex-direction: column;
      gap: 8px;
      min-height: 0;
      user-select: text;
    }
    #vz-json-input {
      flex: 1;
      width: 100%;
      resize: none;
      background: #111;
      color: #b6ffb0;
      border: 1px solid #444;
      border-radius: 4px;
      font-family: "Consolas", "Menlo", monospace;
      font-size: 12px;
      padding: 8px;
      box-sizing: border-box;
      white-space: pre;
      min-height: 60px;
    }
    #vz-json-input:focus { outline: none; border-color: #6cf; }
    #vz-json-label {
      font-size: 11px;
      color: #888;
      flex-shrink: 0;
      margin-top: 2px;
    }
    #vz-json-received {
      flex: 1;
      width: 100%;
      resize: none;
      background: #111;
      color: #ffd479;
      border: 1px solid #444;
      border-radius: 4px;
      font-family: "Consolas", "Menlo", monospace;
      font-size: 12px;
      padding: 8px;
      box-sizing: border-box;
      white-space: pre;
      min-height: 60px;
    }
    #vz-json-actions { display: flex; gap: 8px; flex-shrink: 0; }
    #vz-json-send, #vz-json-format {
      flex: 1;
      padding: 6px 0;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 12px;
    }
    #vz-json-send { background: #3a7afe; color: #fff; }
    #vz-json-send:hover { background: #558cff; }
    #vz-json-format { background: #333; color: #ccc; }
    #vz-json-format:hover { background: #444; }
    #vz-json-status { font-size: 11px; min-height: 14px; color: #999; flex-shrink: 0; }
    #vz-json-status.vz-error { color: #ff6b6b; }
    #vz-json-status.vz-ok { color: #6bff8f; }
    #vz-json-log {
      white-space: pre-wrap;
      background: #0a0a0a;
      font-family: "Consolas", "Menlo", monospace;
      font-size: 11px;
      padding: 6px;
      height: 80px;
      overflow-y: auto;
      border: 1px solid #333;
      border-radius: 4px;
      flex-shrink: 0;
    }
    #vz-json-log .vz-log-out { color: #7fd7ff; }
    #vz-json-log .vz-log-in { color: #ffd479; }
    #vz-json-toggle {
      position: fixed;
      bottom: 20px;
      right: 20px;
      z-index: 100000;
      display: flex;
      align-items: center;
      gap: 6px;
      background: rgba(20, 20, 20, 0.92);
      border: 1px solid #444;
      border-radius: 20px;
      padding: 6px 12px;
      font-family: -apple-system, "Segoe UI", sans-serif;
      font-size: 12px;
      color: #eee;
      cursor: pointer;
      user-select: none;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.5);
    }
    #vz-json-toggle input[type="checkbox"] { cursor: pointer; }
  `;

  var styleEl = document.createElement("style");
  styleEl.id = "vz-json-panel-style";
  styleEl.textContent = css;
  document.head.appendChild(styleEl);

  // -------------------- 2. 建立 DOM --------------------
  var panel = document.createElement("div");
  panel.id = "vz-json-panel";
  panel.innerHTML =
    '<div id="vz-json-header"><span>Unity Bridge</span></div>' +
    '<div id="vz-json-body">' +
    '  <div id="vz-json-label">傳送到 Unity</div>' +
    '  <textarea id="vz-json-input" spellcheck="false" placeholder="輸入要傳給 Unity 的內容（純文字或 JSON 皆可）"></textarea>' +
    '  <div id="vz-json-status"></div>' +
    '  <div id="vz-json-actions">' +
    '    <button id="vz-json-format">格式化 JSON</button>' +
    '    <button id="vz-json-send">傳送到 Unity</button>' +
    "  </div>" +
    '  <div id="vz-json-label">接收自 Unity（最新一筆）</div>' +
    '  <textarea id="vz-json-received" spellcheck="false" readonly placeholder="尚未收到 Unity 傳來的資料"></textarea>' +
    '  <div id="vz-json-log"></div>' +
    "</div>";

  var toggle = document.createElement("label");
  toggle.id = "vz-json-toggle";
  toggle.innerHTML =
    '<input type="checkbox" id="vz-json-toggle-checkbox" checked /> Bridge 視窗';

  function mount() {
    document.body.appendChild(panel);
    document.body.appendChild(toggle);
    initLogic();
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", mount);
  } else {
    mount();
  }

  // -------------------- 3. 邏輯 --------------------
  function initLogic() {
    var header = document.getElementById("vz-json-header");
    var input = document.getElementById("vz-json-input");
    var status = document.getElementById("vz-json-status");
    var sendBtn = document.getElementById("vz-json-send");
    var formatBtn = document.getElementById("vz-json-format");
    var logEl = document.getElementById("vz-json-log");
    var receivedEl = document.getElementById("vz-json-received");
    var toggleCheckbox = document.getElementById("vz-json-toggle-checkbox");

    function log(msg, direction) {
      var line = document.createElement("div");
      line.className = direction === "in" ? "vz-log-in" : "vz-log-out";
      line.textContent = msg;
      logEl.appendChild(line);
      logEl.scrollTop = logEl.scrollHeight;
    }

    function setStatus(msg, type) {
      status.textContent = msg;
      status.className = type ? "vz-" + type : "";
    }

    function getUnityInstance() {
      if (typeof unityInstance !== "undefined" && unityInstance) return unityInstance;
      if (window.unityInstance) return window.unityInstance;
      return null;
    }

    // 顯示 / 隱藏
    toggleCheckbox.addEventListener("change", function () {
      panel.classList.toggle("vz-hidden", !toggleCheckbox.checked);
    });

    // 拖曳
    var dragState = { dragging: false, offsetX: 0, offsetY: 0 };

    header.addEventListener("pointerdown", function (e) {
      dragState.dragging = true;
      var rect = panel.getBoundingClientRect();
      dragState.offsetX = e.clientX - rect.left;
      dragState.offsetY = e.clientY - rect.top;
      header.setPointerCapture(e.pointerId);
    });

    header.addEventListener("pointermove", function (e) {
      if (!dragState.dragging) return;
      var x = e.clientX - dragState.offsetX;
      var y = e.clientY - dragState.offsetY;

      var maxX = window.innerWidth - panel.offsetWidth;
      var maxY = window.innerHeight - panel.offsetHeight;
      x = Math.max(0, Math.min(x, maxX));
      y = Math.max(0, Math.min(y, maxY));

      panel.style.left = x + "px";
      panel.style.top = y + "px";
      panel.style.right = "auto";
    });

    header.addEventListener("pointerup", function (e) {
      dragState.dragging = false;
      header.releasePointerCapture(e.pointerId);
    });

    // 格式化 JSON（僅輔助，不影響是否能傳送）
    formatBtn.addEventListener("click", function () {
      try {
        var parsed = JSON.parse(input.value);
        input.value = JSON.stringify(parsed, null, 2);
        setStatus("JSON 格式正確", "ok");
      } catch (err) {
        setStatus("不是合法 JSON（純文字仍可直接傳送）", "error");
      }
    });

    // 傳送到 Unity：任意文字內容皆可
    // 優先使用專案既有的 SendToUnityByCustom()（unity-bridge-core.js），
    // 這樣會自動吃到它的「未就緒訊息暫存 / 就緒後補送」機制；
    // 若該函式不存在（例如單獨測試這支面板），退回直接呼叫 unityInstance.SendMessage。
    sendBtn.addEventListener("click", function () {
      var raw = input.value;
      if (!raw) return;

      var objName = typeof UnityObjName !== "undefined" ? UnityObjName : UNITY_OBJECT_NAME;
      var methodName = typeof UnityMethodName !== "undefined" ? UnityMethodName : UNITY_METHOD_NAME;

      if (typeof SendToUnityByCustom === "function") {
        var ready = !!(window.unityReady && window.unityInstance);
        SendToUnityByCustom(objName, methodName, raw);
        setStatus(ready ? "已送出" : "Unity 尚未就緒，已加入佇列", ready ? "ok" : "error");
        log("[" + nowTime() + "] 送出 -> Unity" + (ready ? "" : "（佇列中）") + ":\n" + raw, "out");
        return;
      }

      // fallback：沒有 unity-bridge-core.js 時的陽春送法
      var instance = getUnityInstance();
      if (!instance) {
        setStatus("Unity 尚未載入完成", "error");
        return;
      }
      instance.SendMessage(objName, methodName, raw);
      setStatus("已送出", "ok");
      log("[" + nowTime() + "] 送出 -> Unity:\n" + raw, "out");
    });

    // -------------------- 接收 Unity -> JS --------------------
    // 專案實際的接收機制是 Unity 呼叫 window.UnityBridge[functionName](payload)
    // （對應 unity-bridge-handlers.js 裡各個具名 handler，如 OnUnityReady、SendToJS）。
    // 這裡用 Proxy 掛勾 window.UnityBridge：不需要修改 core.js / handlers.js，
    // 任何現在有的、以後新增的 handler 被呼叫時都會自動被記錄下來。
    hookUnityBridge(function (functionName, payload) {
      var text = formatPayload(payload);
      receivedEl.value = "[" + functionName + "]\n" + text;
      log("[" + nowTime() + "] 收到 <- Unity [" + functionName + "]:\n" + text, "in");
    });

    // 仍保留 window.onUnityMessage 相容路徑（若有其他 jslib 直接呼叫它）
    window.onUnityMessage = function (message) {
      receivedEl.value = message;
      log("[" + nowTime() + "] 收到 <- Unity:\n" + message, "in");
    };

    function formatPayload(payload) {
      if (payload === undefined) return "(無 payload)";
      if (typeof payload === "string") {
        try {
          return JSON.stringify(JSON.parse(payload), null, 2);
        } catch (e) {
          return payload;
        }
      }
      try {
        return JSON.stringify(payload, null, 2);
      } catch (e) {
        return String(payload);
      }
    }

    function hookUnityBridge(onReceive) {
      if (window.__vzBridgeHooked) return;
      window.__vzBridgeHooked = true;

      var target = window.UnityBridge || {};
      window.UnityBridge = new Proxy(target, {
        get: function (obj, prop) {
          var value = obj[prop];
          if (typeof value === "function") {
            return function () {
              var payload = arguments.length > 0 ? arguments[0] : undefined;
              try {
                onReceive(String(prop), payload);
              } catch (e) {
                console.error("[vz-bridge-panel] onReceive 錯誤:", e);
              }
              return value.apply(obj, arguments);
            };
          }
          return value;
        },
        set: function (obj, prop, value) {
          obj[prop] = value;
          return true;
        },
      });
    }

    function nowTime() {
      var d = new Date();
      function pad(n) { return String(n).padStart(2, "0"); }
      return pad(d.getHours()) + ":" + pad(d.getMinutes()) + ":" + pad(d.getSeconds());
    }
  }
})();
