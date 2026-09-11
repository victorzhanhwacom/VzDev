using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using VzDev.ApiExtensions;
using VzDev.UnityAPI.Extensions;

namespace VzDev.Net.WebAPI
{
    public class WebApiCallHandler : MonoBehaviour
    {
        public void SetBodyRawJson(string bodyRawJson) => webApiRequestSo.SetBodyRawJson(bodyRawJson);

        public void SetAuthorizationToken(string token, EnumAuthorizationType authorizationType) =>
            webApiRequestSo.SetAuthorizationToken(token, authorizationType);

        [Button]
        public void CallWebAPI()
        {
            responseString = string.Empty;
            webApiRequestSo.CallAPI(OnSuccessHandler, OnFailedHandler);
        }

        private void OnSuccessHandler(string jsonString)
        {
            if (webApiRequestSo.ResponseType == EnumResponseDataType.Json) jsonString = jsonString.ToJsonFormat();
            responseString = jsonString;
            onSuccess?.Invoke(jsonString);
        }

        private void OnFailedHandler(string msg)
        {
            responseString = msg;
            onFailed?.Invoke(msg);
        }

        #region Variables

        [Foldout("[Event] - 成功時Invoke (JSON字串)")]
        public UnityEvent<string> onSuccess;

        [Foldout("[Event] - 失敗時Invoke (錯誤訊息)")] public UnityEvent<string> onFailed;
        [Label("Request請求包"), SerializeField, Expandable] private WebApiRequestSO webApiRequestSo;

        [Foldout("[Response訊息]"), SerializeField, TextArea(1, 10), ShowIf(nameof(IsHaveResponse))]
        private string responseString;

        private bool IsHaveResponse
        {
            get
            {
                if (string.IsNullOrEmpty(responseString)) return false;
                return responseString.Trim().Length > 0;
            }
        }

        #endregion
    }
}