using NaughtyAttributes;
using UnityEngine;

// 掛在設備定位點上，負責設備的位置、朝向與靠齊，轉向後會重新計算
public class RackDevice : MonoBehaviour
{
    RackUGrid rack;
    Transform model;
    RackDeviceSpawner.FitMode fitMode;
    RackPlacement placement;

    public RackPlacement Placement => placement;
    public Transform Model => model;

    // 預設朝向：前門上架面朝前（0 度），後門上架面朝後（180 度）
    int DefaultSteps => placement.depthAlign == RackDepthAlign.Back ? 2 : 0;
    public int TotalSteps => Mod4(DefaultSteps + placement.yawSteps);
    public float Yaw => TotalSteps * 90f;

    public void Init(RackUGrid rack, Transform model, RackPlacement p, RackDeviceSpawner.FitMode fitMode)
    {
        this.rack = rack;
        this.model = model;
        this.fitMode = fitMode;
        placement = p;
        placement.yawSteps = Mod4(placement.yawSteps);
        ApplyPose();
    }

    // dir > 0 轉 +90 度，dir < 0 轉 -90 度
    public void Rotate(int dir)
    {
        if (dir == 0) return;
        placement.yawSteps = Mod4(placement.yawSteps + (dir > 0 ? 1 : -1));
        ApplyPose();
    }

    // 回到預設朝向
    public void ResetRotation()
    {
        placement.yawSteps = 0;
        ApplyPose();
    }

    public void ApplyPose()
    {
        rack.GetLocalPose(placement, out var slotCenter, out var slotSize);
        transform.localPosition = slotCenter;
        transform.localRotation = Quaternion.identity;

        model.localPosition = Vector3.zero;
        model.localRotation = Quaternion.Euler(0f, Yaw, 0f);
        model.localScale = Vector3.one;

        // 在定位點空間量包圍盒，已包含旋轉
        if (!BoundsUtil.TryGetLocalBounds(transform, out var b)) return;

        if (fitMode == RackDeviceSpawner.FitMode.StretchToSlot)
        {
            Vector3 f = new Vector3(
                slotSize.x / Mathf.Max(b.size.x, 0.0001f),
                slotSize.y / Mathf.Max(b.size.y, 0.0001f),
                slotSize.z / Mathf.Max(b.size.z, 0.0001f));
            // 轉 90/270 度時，模型的 X 軸對到定位點的 Z 軸，縮放要互換
            model.localScale = (TotalSteps & 1) == 1 ? new Vector3(f.z, f.y, f.x) : f;
            BoundsUtil.TryGetLocalBounds(transform, out b);
        }

        Vector3 pos = -b.center;
        pos.z += DepthOffset(slotSize.z, b.size.z);
        model.localPosition = pos;
    }

    // 從槽中心往前/後推多少，讓設備貼齊前門或後門
    // 設備比機櫃深時 gap 為負，會從另一側凸出，符合實際上架情況
    float DepthOffset(float slotDepth, float deviceDepth)
    {
        float gap = (slotDepth - deviceDepth) * 0.5f;
        return placement.depthAlign switch
        {
            RackDepthAlign.Front =>  gap * rack.FrontSign,
            RackDepthAlign.Back  => -gap * rack.FrontSign,
            _ => 0f
        };
    }

    static int Mod4(int v) => ((v % 4) + 4) % 4;

    
    [Button]
    private void RotateLeft() => Rotate(-1);
    [Button]
    private void RotateRight() => Rotate(1);
}