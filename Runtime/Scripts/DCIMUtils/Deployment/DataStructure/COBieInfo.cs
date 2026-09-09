using System;

namespace VzDev.DCIMUtils.DataUtils
{
    /// <summary>
    /// COBie資料結構
    /// </summary>
    [Serializable]
    public class COBieInfo
    {
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
        public string document_inspection = "";
        public string document_handout = "";
        public string document_drawing = "";
        public string contact_company = "";
        public string contact_department = "";
        public string contact_email = "";
        public string contact_familyName = "";
        public string contact_givenName = "";
        public string contact_phone = "";
        public string contact_street = "";
        public string facility_name = "";
        public string facility_projectName = "";
        public string facility_siteName = "";
        public string equipment_supplier = "";
        public string floor_name = "";
        public string space_name = "";
        public string space_roomTag = "";
        public string system_category = "";
        public string system_name = "";
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

        internal static COBieInfo ToClone(COBieInfo cobieInfo)
        {
            COBieInfo newData = new COBieInfo
            {
                component_description = cobieInfo.component_description,
                component_assetIdentifier = cobieInfo.component_assetIdentifier,
                component_serialNumber = cobieInfo.component_serialNumber,
                component_installationDate = cobieInfo.component_installationDate,
                component_tagName = cobieInfo.component_tagName,
                component_warrantyDurationPart = cobieInfo.component_warrantyDurationPart,
                component_warrantyDurationUnit = cobieInfo.component_warrantyDurationUnit,
                component_warrantyGuarantorLabor = cobieInfo.component_warrantyGuarantorLabor,
                component_warrantyStartDate = cobieInfo.component_warrantyStartDate,
                component_warrantyEndDate = cobieInfo.component_warrantyEndDate,
                document_inspection = cobieInfo.document_inspection,
                document_handout = cobieInfo.document_handout,
                document_drawing = cobieInfo.document_drawing,
                contact_company = cobieInfo.contact_company,
                contact_department = cobieInfo.contact_department,
                contact_email = cobieInfo.contact_email,
                contact_familyName = cobieInfo.contact_familyName,
                contact_givenName = cobieInfo.contact_givenName,
                contact_phone = cobieInfo.contact_phone,
                contact_street = cobieInfo.contact_street,
                facility_name = cobieInfo.facility_name,
                facility_projectName = cobieInfo.facility_projectName,
                facility_siteName = cobieInfo.facility_siteName,
                equipment_supplier = cobieInfo.equipment_supplier,
                floor_name = cobieInfo.floor_name,
                space_name =cobieInfo.space_name,
                space_roomTag =cobieInfo.space_roomTag,
                system_category =cobieInfo.system_category,
                system_name =cobieInfo.system_name,
                type_category =cobieInfo.type_category,
                type_expectedLife =cobieInfo.type_expectedLife,
                type_manufacturer =cobieInfo.type_manufacturer,
                type_modelNumber =cobieInfo.type_modelNumber,
                type_name =cobieInfo.type_name,
                type_replacementCost =cobieInfo.type_replacementCost,
                type_accessibilityPerformance =cobieInfo.type_accessibilityPerformance,
                type_shape =cobieInfo.type_shape,
                type_size =cobieInfo.type_size,
                type_color =cobieInfo.type_color,
                type_finish =cobieInfo.type_finish,
                type_grade =cobieInfo.type_grade,
                type_material =cobieInfo.type_material
            };
            return newData;     
        }
    }
}