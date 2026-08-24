using System;
using System.Collections.Generic;

namespace VzDev.DCIMUtils.DataUtils
{
    /// <summary>
    /// 機櫃資產資料 (DCR專用) - DTO
    /// <para>【用途】用於 WebAPI 回傳的 json 反序列化</para>
    /// </summary>
    [Serializable]
    public class DCR_Asset_DTO
    {
        public string devicePath;
        public InformationDto information;
        public List<EquipmentAsset> containers = new();

        public DCR_Asset ToDCRAsset()
        {
            var asset = new DCR_Asset
            {
                deviceCode = devicePath,
                cobieInfo = information?.ToCOBieInfo(),
                weight_kg_Max = information?.weight_limit ?? 0,
                power_watt_Max = information?.watt_limit ?? 0,
                u_height_Max = information?.heightU ?? 0,
                weight_kg = information?.weight ?? 0,
                container = containers ?? new List<EquipmentAsset>()
            };
            return asset;
        }
    }

    public class InformationDto
    {
        public COBieInfo ToCOBieInfo()
        {
            return new COBieInfo
            {
                component_description = component_description,
                component_assetIdentifier = component_assetIdentifier,
                component_serialNumber = component_serialNumber,
                component_installationDate = component_installationDate,
                component_tagName = component_tagName,
                component_warrantyDurationPart = component_warrantyDurationPart,
                component_warrantyDurationUnit = component_warrantyDurationUnit,
                component_warrantyGuarantorLabor = component_warrantyGuarantorLabor,
                component_warrantyStartDate = component_warrantyStartDate,
                component_warrantyEndDate = component_warrantyEndDate,
                document_inspection = document_inspection,
                document_handout = document_handout,
                document_drawing = document_drawing,
                contact_company = contact_company,
                contact_department = contact_department,
                contact_email = contact_email,
                contact_familyName = contact_familyName,
                contact_givenName = contact_givenName,
                contact_phone = contact_phone,
                contact_street = contact_street,
                facility_name = facility_name,
                facility_projectName = facility_projectName,
                facility_siteName = facility_siteName,
                equipment_supplier = equipment_supplier,
                floor_name = floor_name,
                space_name = space_name,
                space_roomTag = space_roomTag,
                system_category = system_category,
                system_name = system_name,
                type_category = type_category,
                type_expectedLife = type_expectedLife,
                type_manufacturer = type_manufacturer,
                type_modelNumber = type_modelNumber,
                type_name = type_name,
                type_replacementCost = type_replacementCost,
                type_accessibilityPerformance = type_accessibilityPerformance,
                type_shape = type_shape,
                type_size = type_size,
                type_color = type_color,
                type_finish = type_finish,
                type_grade = type_grade,
                type_material = type_material
            };
        }

        /// <summary>
        /// ToAsset() 的反向轉換：COBieInfo → InformationDto。
        /// 兩者欄位名稱完全對應（都是COBie標準欄位），逐一複製即可，
        /// 不需要額外的比對表。用於 RackAssetJsonConverter.ConvertToDto 匯出JSON時，
        /// 把 DCR_Asset.cobieInfo 還原回 DTO 格式。
        /// </summary>
        public static InformationDto FromCOBieInfo(COBieInfo cobie)
        {
            if (cobie == null) return new InformationDto();

            return new InformationDto
            {
                component_description = cobie.component_description,
                component_assetIdentifier = cobie.component_assetIdentifier,
                component_serialNumber = cobie.component_serialNumber,
                component_installationDate = cobie.component_installationDate,
                component_tagName = cobie.component_tagName,
                component_warrantyDurationPart = cobie.component_warrantyDurationPart,
                component_warrantyDurationUnit = cobie.component_warrantyDurationUnit,
                component_warrantyGuarantorLabor = cobie.component_warrantyGuarantorLabor,
                component_warrantyStartDate = cobie.component_warrantyStartDate,
                component_warrantyEndDate = cobie.component_warrantyEndDate,
                document_inspection = cobie.document_inspection,
                document_handout = cobie.document_handout,
                document_drawing = cobie.document_drawing,
                contact_company = cobie.contact_company,
                contact_department = cobie.contact_department,
                contact_email = cobie.contact_email,
                contact_familyName = cobie.contact_familyName,
                contact_givenName = cobie.contact_givenName,
                contact_phone = cobie.contact_phone,
                contact_street = cobie.contact_street,
                facility_name = cobie.facility_name,
                facility_projectName = cobie.facility_projectName,
                facility_siteName = cobie.facility_siteName,
                equipment_supplier = cobie.equipment_supplier,
                floor_name = cobie.floor_name,
                space_name = cobie.space_name,
                space_roomTag = cobie.space_roomTag,
                system_category = cobie.system_category,
                system_name = cobie.system_name,
                type_category = cobie.type_category,
                type_expectedLife = cobie.type_expectedLife,
                type_manufacturer = cobie.type_manufacturer,
                type_modelNumber = cobie.type_modelNumber,
                type_name = cobie.type_name,
                type_replacementCost = cobie.type_replacementCost,
                type_accessibilityPerformance = cobie.type_accessibilityPerformance,
                type_shape = cobie.type_shape,
                type_size = cobie.type_size,
                type_color = cobie.type_color,
                type_finish = cobie.type_finish,
                type_grade = cobie.type_grade,
                type_material = cobie.type_material
            };
        }

        #region 機櫃上限與目前用量
        public int watt_limit;
        public float weight_limit;
        public int heightU;

        /// <summary>
        /// 後端目前不會正確回傳，不映射到任何欄位，僅接收避免反序列化失敗。
        /// </summary>
        public int watt;
        public float weight;
        #endregion

        #region 尺寸（假設單位為公分，映射到 SizeInfo 時需 ×10 轉換為毫米）
        public float length;
        public float width;
        public float height;
        #endregion

        #region COBie - Component
        public string component_description = "";
        public string component_assetIdentifier = "";
        public string component_serialNumber = "";
        public string component_installationDate = "";
        public string component_tagName = "";
        public string component_warrantyDurationPart = "";
        public string component_warrantyDurationUnit = "";
        public string component_warrantyGuarantorLabor = "";
        public string component_warrantyStartDate = "";
        public string component_warrantyEndDate = "";
        #endregion

        #region COBie - Document
        public string document_inspection = "";
        public string document_handout = "";
        public string document_drawing = "";
        #endregion

        #region COBie - Contact
        public string contact_company = "";
        public string contact_department = "";
        public string contact_email = "";
        public string contact_familyName = "";
        public string contact_givenName = "";
        public string contact_phone = "";
        public string contact_street = "";
        #endregion

        #region COBie - Facility / Equipment / Floor / Space / System
        public string facility_name = "";
        public string facility_projectName = "";
        public string facility_siteName = "";
        public string equipment_supplier = "";
        public string floor_name = "";
        public string space_name = "";
        public string space_roomTag = "";
        public string system_category = "";
        public string system_name = "";
        #endregion

        #region COBie - Type
        public string type_category = "";
        public string type_expectedLife = "";
        public string type_manufacturer = "";
        public string type_modelNumber = "";
        public string type_name = "";
        public string type_replacementCost = "";
        public string type_accessibilityPerformance = "";
        public string type_shape = "";
        public string type_size = "";
        public string type_color = "";
        public string type_finish = "";
        public string type_grade = "";
        public string type_material = "";
        #endregion
    }

}