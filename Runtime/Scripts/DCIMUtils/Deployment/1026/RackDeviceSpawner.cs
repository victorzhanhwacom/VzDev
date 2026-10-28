using NaughtyAttributes;
using UnityEngine;

public class RackDeviceSpawner : MonoBehaviour
{
    public enum FitMode
    {
        KeepOriginalSize,   // 保持模型原尺寸，只對齊中心（建議，真實模型用這個）
        StretchToSlot       // 拉伸到剛好填滿 U 槽（佔位用方塊或測試用）
    }

    [SerializeField] RackUGrid rackGrid;
    [SerializeField] GameObject devicePrefab;
    [SerializeField] FitMode fitMode = FitMode.KeepOriginalSize;

    public RackDevice Spawn(RackPlacement p)
    {
        if (!rackGrid.TryAdd(p))
        {
            Debug.LogWarning($"設備 {p.id} 放不進 U{p.startU}");
            return null;
        }

        // 定位點：放在機櫃底下，位置與朝向由 RackDevice 計算
        var anchor = new GameObject(p.id).transform;
        anchor.SetParent(rackGrid.transform, false);

        var model = Instantiate(devicePrefab, anchor).transform;

        var device = anchor.gameObject.AddComponent<RackDevice>();
        device.Init(rackGrid, model, p, fitMode);
        return device;
    }

    public int startU = 1, sizeU = 1;

    [Button]
    private void SpawnDeviceFull()
    {
        var p = RackPlacement.Full(devicePrefab.name, startU, sizeU);
        Spawn(p);
    }
    public int index = 0, count = 1;
    [Button]
    private void SpawnDeviceColumn()
    {
        var p = RackPlacement.Column(devicePrefab.name, startU, sizeU, index, count);
        Spawn(p);
    }
}