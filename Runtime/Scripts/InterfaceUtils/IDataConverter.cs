using UnityEngine;

namespace VzDev.InterfaceUtils
{
    /// <summary>
    /// 專門用於供資料轉換的介面
    /// <para>+ TData可以是class、struct、SO、Component</para>
    /// <para>+ Convert()裡再自訂定義轉換的邏輯處理</para
    /// </summary>
    public interface IDataConverter<TData>
    {
        TData Convert();
    }
}
