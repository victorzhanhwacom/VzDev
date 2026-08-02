using System;
using NaughtyAttributes;
using UnityEngine;
using VzDev.DCIM.RevitAssetDataStructure;
using VzDev.InteractiveUtils.ModelMouseEvent;
using VzDev.UnityAPI.Extensions;

namespace VzDev.DCIMUtils.ModelInteractUtils
{
    /// <summary>
    /// 掛載資料項到目標模型上，並處理各模型的互動事件，將資料透過 ModelComponentSetterHub 轉發給其他地方訂閱。
    /// </summary>
    public abstract class ModelComponentBase<TData> : MonoBehaviour
        , IModelClick, IModelHover, IModelComponent<TData>, IHasDCIMAsset where TData : DCIMAsset
    {
        #region Fields
        [SerializeField, Tooltip("目標模型攜帶的資料項")] protected TData data;
        /// <summary>
        /// 將Collider獨立出來，提供設定Collider啟用狀態的功能，允許外部控制模型的互動性。
        /// </summary>
        private ModelColliderSetter modelColliderSetter;

        #endregion

        /// <summary>
        /// 型別無關的資料存取入口。因為 TData 在編譯期就固定了，外部（例如只拿到 Renderer/GameObject
        /// 的地方，像 SelectionController 廣播出來的選取結果）沒辦法知道具體是哪個 TData，
        /// 透過這個非泛型介面統一上轉型成 DCIMAsset，就能在不知道具體型別的情況下取出資料。
        /// </summary>
        public DCIMAsset GetAsset() => data;

        public void SetColliderEnabled(bool isEnabled)
        {
            modelColliderSetter ??= new ModelColliderSetter(transform);
            modelColliderSetter.SetColliderEnabled(isEnabled);
        }

        public virtual void SetData(TData assetData)
        {
            data = assetData;
            data.modelInfo.modelTarget = transform;
        }

        #region Events
        public event Action<TData> OnModelClickedEvent;
        public event Action<TData> OnHoverEnterEvent;
        public event Action<TData> OnHoverExitEvent;

        public void OnHoverEnter(GameObject targetObject) => OnHoverEnterEvent?.Invoke(data);
        public void OnHoverExit(GameObject targetObject) => OnHoverExitEvent?.Invoke(data);
        public void OnModelClicked(GameObject clickedObject) => OnModelClickedEvent?.Invoke(data);
        #endregion
    }

    /// <summary>
    /// 將Collider獨立出來，提供設定Collider啟用狀態的功能，允許外部控制模型的互動性。
    /// </summary>
    public class ModelColliderSetter
    {
        private Transform model;
        private BoxCollider hitCollider;
        public ModelColliderSetter(Transform target)
        {
            if (target == null) return;
            model = target;
            model.TryAddComponent(out hitCollider);
        }

        public void SetColliderEnabled(bool isEnabled)
        {
            if (hitCollider != null)
                hitCollider.enabled = isEnabled;
        }


    }

    /// <summary>
    /// 型別無關的資料存取介面。任何掛載了 ModelComponentBase&lt;TData&gt; 的模型都會實作這個介面，
    /// 讓外部只要用 TryGetComponent&lt;IHasDCIMAsset&gt; 就能取出資料，不需要知道具體的 TData 型別。
    /// </summary>
    public interface IHasDCIMAsset
    {
        DCIMAsset GetAsset();
    }
    public static class IHasDCIMAssetExtensions
    {
        public static bool TryGetAsset<T>(this IHasDCIMAsset provider, out T asset) where T : DCIMAsset
        {
            asset = provider?.GetAsset() as T;
            return asset != null;
        }
    }

    /// <summary>
    /// 定義目標模型的互動介面
    /// </summary>
    public interface IModelComponent<TData> where TData : DCIMAsset
    {
        /// <summary>
        /// 設定資料項
        /// </summary>
        void SetData(TData assetData);

        /// <summary>
        /// 設定Collider的啟用狀態，允許外部控制模型的互動性。
        /// </summary>
        void SetColliderEnabled(bool isEnabled);
        event Action<TData> OnModelClickedEvent;
        event Action<TData> OnHoverEnterEvent;
        event Action<TData> OnHoverExitEvent;
    }
}