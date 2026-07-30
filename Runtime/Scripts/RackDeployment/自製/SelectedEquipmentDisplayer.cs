using System;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VzDev.DCIMUtils.RackDeployment;

namespace VzDev
{
    public class SelectedEquipmentDisplayer : MonoBehaviour
    {
        [SerializeField, ReadOnly] private EquipmentCatalogEntry currentEquipment;

        [Foldout("[Events]")] public UnityEvent<GameObject> invokeEquipmentModelCreated;
        [Foldout("[Components]"), SerializeField] private TextMeshProUGUI txtSn, txtModel;
        [Foldout("[Components]"), SerializeField] private Image photo;


        public void SetEquipment(EquipmentCatalogEntry catalogEntry)
        {
            currentEquipment = catalogEntry;
            txtSn.text = catalogEntry.assetNoPrefix;
            txtModel.text = catalogEntry.displayName;
            photo.sprite = catalogEntry.icon;
           
            CreateAssetModel();
        }

        private void CreateAssetModel()
        {
           invokeEquipmentModelCreated?.Invoke(currentEquipment.modelPrefab);
        }
    }
}
