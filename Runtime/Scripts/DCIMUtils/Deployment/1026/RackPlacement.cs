public enum RackDepthAlign
{
    Center,  // 前後置中（預設，維持舊行為）
    Front,   // 靠前門對齊（從前門上架）
    Back     // 靠後門對齊（從後門上架）
}

public struct RackPlacement
{
    public string id;
    public int startU, sizeU;
    public float xMin, xMax;          // U 內的水平範圍，0~1
    public RackDepthAlign depthAlign; // 深度方向靠齊方式
    public int yawSteps;              // 使用者額外轉向，每步 90 度（疊加在預設朝向上）

    public static RackPlacement Full(string id, int startU, int sizeU,
                                     RackDepthAlign align = RackDepthAlign.Front)
        => new RackPlacement { id = id, startU = startU, sizeU = sizeU,
                               xMin = 0f, xMax = 1f, depthAlign = align };

    public static RackPlacement Column(string id, int startU, int sizeU, int index, int count,
                                       RackDepthAlign align = RackDepthAlign.Front)
        => new RackPlacement { id = id, startU = startU, sizeU = sizeU,
                               xMin = (float)index / count, xMax = (float)(index + 1) / count,
                               depthAlign = align };
}