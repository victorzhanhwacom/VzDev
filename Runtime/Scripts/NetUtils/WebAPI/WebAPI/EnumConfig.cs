
namespace VzDev.Net.WebAPI
{
    public enum EnumAuthorizationType
    {
        NoAuth,
        Bearer 
    }
    /// Https / Http
    public enum EnumHttpType
    {
        https,
        http
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