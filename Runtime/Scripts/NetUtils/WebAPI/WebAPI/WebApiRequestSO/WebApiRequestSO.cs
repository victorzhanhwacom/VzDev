using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using NaughtyAttributes;
using UnityEngine;
using VzDev.ApiExtensions;
using VictorDev.Net;
using VictorDev.Net.WebAPI;
using VzDev.UnityAPI.Extensions;

namespace VzDev.Net.WebAPI
{
    /// WebAPI Requst資料包
    [CreateAssetMenu(fileName = "WebApiRequest", menuName = "VictorDev/Net/WebApiRequest")]
    public class WebApiRequestSO : ScriptableObject
    { 
        #region Getter & Setter
        private Dictionary<EnumResponseDataType, string> ResponseTypeTable { get; set; }

        public EnumHttpMethod EnumHttpMethod => enumHttpMethod;

        public string URL
        {
            get
            {
                string result;
                if (IsHaveIpConfig)
                {
                    result = extendApiURL.Trim();
                    if (result.StartsWith("/") == false) result = ipConfigSo.URL + "/" + result;
                    else result = ipConfigSo.URL + result;
                }
                else
                {
                    result = fullApiURL.Trim();
                    if (result.StartsWith(httpType.ToString()) == false) result = httpType + "://" + result;
                }
                return result;
            }
        }

        /// 依照HttpMethod(POST/GET...)來設定HttpRequest內容
        public HttpRequestMessage HttpRequestMessage
        {
            get
            {
                HttpRequestMessage request;
                string url = URL;
                if (isQueryParams)
                {
                    if(url.EndsWith("/") == false) url += "/";
                    url += "?";

                    for (var i = 0; i < queryParams.Count; i++)
                    {
                        if(i > 0) url += "&";
                        url += $"{queryParams[i].Key}={queryParams[i].Value}";
                    }
                    Debug.Log($"QueryParams: \turl: {url}\n");
                }
                switch (enumHttpMethod)
                {
                    default:
                    case EnumHttpMethod.GET:
                        request = new HttpRequestMessage(HttpMethod.Get, url);
                        break;
                    case EnumHttpMethod.POST:
                        request = new HttpRequestMessage(HttpMethod.Post, url);
                        switch (bodyType)
                        {
                            case EnumBody.RawJson:
                                request.Content = new StringContent(BodyRawJson, Encoding.UTF8, MediaType);
                                break;
                            case EnumBody.FormData:
                                var form = new MultipartFormDataContent();
                                foreach (KeyValueData<string, string> kv in formData)
                                {
                                    form.Add(new StringContent(kv.Value.Trim()), kv.Key.Trim());
                                }
                                request.Content = form;
                                break;
                        }
                        break;
                    case EnumHttpMethod.PATCH:
                        request = new HttpRequestMessage(HttpMethod.Patch, url);
                        switch (bodyType)
                        {
                            case EnumBody.RawJson:
                                StringContent content = new StringContent(BodyRawJson, Encoding.UTF8, MediaType);
                                
                                // PATCH 很常要求 Accept
                                content.Headers.ContentType.CharSet = "utf-8";
                                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaType));
                                request.Content = content;
                                break;
                            case EnumBody.FormData:
                                var form = new MultipartFormDataContent();
                                foreach (KeyValueData<string, string> kv in formData) 
                                    form.Add(new StringContent(kv.Value.Trim()), kv.Key.Trim());
                                request.Content = form;
                                break;
                        }
                        break;
                }

                // 設定Authorization
                if (authorizationSo != null && authorizationSo.AuthorizationType != EnumAuthorizationType.NoAuth)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue(
                        authorizationSo.AuthorizationType.ToString(), authorizationSo.Token);
                }
                return request;
            }
        }

        /// 回應資料的型態
        public string MediaType
        {
            get
            {
                ResponseTypeTable ??= new Dictionary<EnumResponseDataType, string>()
                {
                    { EnumResponseDataType.Json, "application/json" },
                    { EnumResponseDataType.WWWForm, "application/x-www-form-urlencoded" },
                    { EnumResponseDataType.Text, "text/plain" },
                };
                return ResponseTypeTable[responseDataType];
            }
        }

        /// form-data型態的參數值
        public WWWForm BodyFormData
        {
            get
            {
                if (CheckBodyType(EnumBody.FormData) == false) return null;

                WWWForm result = new WWWForm();
                formData.ForEach(item =>
                {
                    Debug.Log($"\t[form-data] {item.Key.Trim()} = {item.Value.Trim()}");
                    result.AddField(item.Key.Trim(), item.Value.Trim());
                });
                return result;
            }
        }

        public string BodyRawJson
        {
            get
            {
                if (CheckBodyType(EnumBody.RawJson) == false) return System.String.Empty;
                return rawJson.Trim();
            }
        }

        private bool CheckBodyType(EnumBody target)
        {
            if (bodyType != target)
            {
                Debug.LogWarning($"{name} is not {target} type.");
                return false;
            }

            return true;
        }
        #endregion

        /// 設定URL (強制取消IPConfig設定)
        public void SetUrl(string url, EnumHttpMethod method = EnumHttpMethod.GET)
        {
            url = url.Trim();
            ipConfigSo = null;
            httpType = url.StartsWith("https://")? EnumHttpType.https : EnumHttpType.http;
            fullApiURL = url;
            enumHttpMethod = method;
        }
        
        /// 設定Get的QueryParams
        public void SetQueryParams(Dictionary<string, string> data)
            => SetQueryParams(data
                .Select(keyPair => new KeyValueData<string, string>(keyPair.Key.Trim(), keyPair.Value.Trim()))
                .ToList());

        /// 設定Get的QueryParams
        public void SetQueryParams(List<KeyValueData<string, string>> data)
        {
            if (data == null)
            {
                Debug.LogWarning($"SetQueryParams: data is null.");
                return;
            }
            queryParams = data;
            enumHttpMethod = EnumHttpMethod.GET;
            isQueryParams = true;
        }

        /// 設定Body內容 (JSON格式)
        public void SetBodyRawJson(string json)
        {
            rawJson = json.Trim().ToJsonFormat();
            bodyType = EnumBody.RawJson;
        }
        
        /// 設定Token
        public void SetAuthorizationToken(string token, EnumAuthorizationType authorizationType) 
            => authorizationSo?.SetToken(token, authorizationType);
        
        /// 呼叫WebAPI
        public void CallAPI(Action<string> onSuccess, Action<string> onFailed = null) 
            => WebApiCaller.SendRequest(this, onSuccess, onFailed); 
        [Button]
        private void CallAPI() => CallAPI(null);
        [Button]
        private void LogURL() => Debug.Log($"URL:  {URL}");
        
        #region Variables
        [Label("[IP Config設定 (選填)]")]
        [SerializeField] private IPConfigSO ipConfigSo;
        private bool IsHaveIpConfig => ipConfigSo != null;
        private bool IsIpConfigNotSetup => ipConfigSo == null;
        [ShowIf(nameof(IsHaveIpConfig))]
        [SerializeField] private string extendApiURL;
        
        [Space(20)]
        [SerializeField] private WebApiAuthorizationSO authorizationSo;
        
        [Space(20)]
        [ShowIf(nameof(IsIpConfigNotSetup))]
        [SerializeField] private EnumHttpType httpType;
        [ShowIf(nameof(IsIpConfigNotSetup))]
        [SerializeField] private string fullApiURL;
        [Space(20)]
        [SerializeField] private EnumHttpMethod enumHttpMethod;
        [SerializeField] private EnumResponseDataType responseDataType;
        public EnumResponseDataType ResponseType => responseDataType;


        #region [Params設定]

        [Space(10)] [Label("[Params設定]")] [SerializeField]
        private bool isQueryParams = false;

        [ShowIf(nameof(isQueryParams))] [SerializeField]
        private List<KeyValueData<string, string>> queryParams;

        [Space(10)]

        #endregion

        #region [Send Body設定]

        [Label("[Send Body設定]")]
        [SerializeField]
        private EnumBody bodyType = EnumBody.None;

        private bool IsFormData => bodyType == EnumBody.FormData;
        private bool IsRaw => bodyType == EnumBody.RawJson;

        [ShowIf(nameof(IsFormData))] [SerializeField]
        private List<KeyValueData<string, string>> formData;

        [ShowIf(nameof(IsRaw))] [ResizableTextArea] [SerializeField]
        private string rawJson;
        #endregion

        #endregion
    }
}