using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VzDev.DCIMUtils.RackDeployment;

public class DeployDevicePanel : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;

    [SerializeField, ReadOnly] private DeviceListItemView[] toggles;

    public UnityEvent<EquipmentCatalogEntry> onToggleSelected;

    private void Start() => GetToggles();

    private void OnEnable()
    {
        for(int i = 0; i < toggles.Length; i++)
        {
            int index = i;
            toggles[index].onToggleSelected.AddListener(onToggleSelected.Invoke);
        }
    }
    private void OnDisable()
    {
        for(int i = 0; i < toggles.Length; i++)
        {
            int index = i;
            toggles[index].onToggleSelected.RemoveListener(onToggleSelected.Invoke);
        }
    }

    [Button]
    private void GetToggles()
    {
        toggles = scrollRect.content.GetComponentsInChildren<DeviceListItemView>(true);
    }
}
