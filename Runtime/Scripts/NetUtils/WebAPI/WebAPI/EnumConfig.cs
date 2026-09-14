
namespace VzDev.NetUtils
{
    public enum EnumAuthorizationType
    {
        NoAuth,
        Bearer 
    }
    /// Https / Http
    public enum EnumHttpType
    {
        http,
        https,
    };
    /// GET / POST 與其它類型
    public enum EnumHttpMethod
    {
        GET,
        POST,
        PUT,
        PATCH, 
        DELETE,
        HEAD,
        OPTIONS,
    }
    public enum EnumBody
    {
        None,
        FormData,
        RawJson,
        RawText,
        Binary
    }

    public enum EnumResponseDataType
    {
        Json,
        WWWForm,
        Text,
        Excel,
        PDF,
        Image,
        Word,
        ZIP,
        Binary,
    }
}