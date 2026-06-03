mergeInto(LibraryManager.library, {

    // 範例 1：跳出網頁原生警示視窗
    ShowAlert: function (str) {
        window.alert(UTF8ToString(str)); // 使用 UTF8ToString 轉換 Unity 傳過來的字串指针
    },

    // 範例 2：將資料傳給網頁上的自訂 JavaScript 函式
    SendDataToPage: function (score) {
        if (typeof window.onUnityScoreUpdate === 'function') {
            window.onUnityScoreUpdate(score);
        }
    }
});