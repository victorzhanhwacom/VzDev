using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace VzDev.DCIMUtils.DataUtils
{
    public class PointModelDataHandler_RTRH_Manager : MonoBehaviour
    {
         #region Fields
        [SerializeField, OnValueChanged("OnRtRhTypeChanged")] private EnumRtRhType rtRhType = EnumRtRhType.Unselect;
        private void OnRtRhTypeChanged() => UpdateHeatSource();

        [SerializeField] private List<PointModelDataHandler_RTRH> pointModelHandlers = new();
        #endregion
        
        public void SetHandlers(List<GameObject> handlers)
        {
            pointModelHandlers = new List<PointModelDataHandler_RTRH>();
            for(int i = 0; i < handlers.Count; i++) 
            {
                var handler = handlers[i].GetComponent<PointModelDataHandler_RTRH>();
                if(handler != null) pointModelHandlers.Add(handler);
            }
            UpdateHeatSource();
        }

         private void UpdateHeatSource()
        {
            if(pointModelHandlers == null || pointModelHandlers.Count == 0) return;
            for(int i = 0; i < pointModelHandlers.Count; i++)
            {
                var handler = pointModelHandlers[i];
                if(handler == null) continue;
                handler.SetRtRhType(rtRhType);
            }
        }

        public void SetRtMode()
        {
            rtRhType = EnumRtRhType.Rt;
            UpdateHeatSource();
        }
        public void SetRhMode()
        {
            rtRhType = EnumRtRhType.Rh;
            UpdateHeatSource();
        }
    }
}
