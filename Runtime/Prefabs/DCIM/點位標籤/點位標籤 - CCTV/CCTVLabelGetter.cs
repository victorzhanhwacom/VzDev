
using System.Linq;
using UnityEngine;
using VzDev.DCIMUtils;

namespace VzDev.ToolUtils
{

    /// <summary>
    /// For CCTV管理
    /// <para>ex: 門禁裝置_Hanwha-QNV-C8013R-半球型_Hanwha-QNV-C8013R-半球型[TG+TPE+IDC+15F+AI機房+E+Hanwha-QNV-C8013R-半球型: Hanwha-QNV-C8013R-半球型+CCTV-15F-14] </para>
    /// </summary>
    public class CCTVLabelGetter : MonoBehaviour, IPointTagLabelGetter
    {
        public string GetLabel(Transform targetModel)
        {
            string deviceID = RevitHelper.GetDeviceID(targetModel.name);
            string roomName = deviceID.Split('+')[4];
            string index = deviceID.Split('-').Last();
            return $"{roomName}{index}";
        }
    }
}