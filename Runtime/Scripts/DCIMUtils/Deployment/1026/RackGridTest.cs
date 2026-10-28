using UnityEngine;
using VzDev.UnityAPI.Extensions;

public class RackGridTest : MonoBehaviour
{
    void Update()
    {
        //if (!Input.GetMouseButtonDown(0)) return;
        if (!Input.GetMouseButton(0)) return;

        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit))
        {
            Debug.Log($"hit:{hit.transform.name}");
            // 看hit物件是否有rackGrid

            if(hit.transform.TryGetComponent(out RackUGrid rackGrid))
            {
                int u = rackGrid.WorldToU(hit.point);
                if (u > 0) Debug.Log($"點到 U{u}");
            }
        }
    }
}
