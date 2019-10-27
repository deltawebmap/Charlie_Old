using DeltaDataExtractor.Entities;
using LibDeltaSystem.Entities.ArkEntries;
using LibDeltaSystem.Entities.ArkEntries.Dinosaur;
using System;
using System.Collections.Generic;
using System.Text;
using UassetToolkit;
using UassetToolkit.UPropertyTypes;
using UassetToolkit.UStructTypes;

namespace ArkImportTools.OutputEntities
{
    public static class ArkItemEntryReader
    {
        public static ItemEntry ConvertEntry(UAssetFileBlueprint bp, UAssetCacheBlock cache, DeltaExportPatch patch)
        {
            //Open reader
            PropertyReader reader = new PropertyReader(bp.GetFullProperties(cache));

            //Get primary icon
            DeltaAsset icon;
            if (reader.GetProperty<ObjectProperty>("ItemIcon") != null)
                icon = ImageTool.QueueImage(reader.GetProperty<ObjectProperty>("ItemIcon").GetReferencedFile(), ImageTool.ImageModifications.None, patch);
            else
                icon = DeltaAsset.MISSING_ICON;

            //Get broken icon
            DeltaAsset brokenIcon;
            if (reader.GetProperty<ObjectProperty>("BrokenImage") != null)
                brokenIcon = ImageTool.QueueImage(reader.GetProperty<ObjectProperty>("BrokenImage").GetReferencedFile(), ImageTool.ImageModifications.None, patch);
            else
                brokenIcon = DeltaAsset.MISSING_ICON;

            //Get the array of UseItemAddCharacterStatusValues
            ArrayProperty statusValuesArray = reader.GetProperty<ArrayProperty>("UseItemAddCharacterStatusValues");
            Dictionary<string, ItemEntry_ConsumableAddStatusValue> statusValues = new Dictionary<string, ItemEntry_ConsumableAddStatusValue>();
            if (statusValuesArray != null)
            {
                foreach(var i in statusValuesArray.props)
                {
                    StructProperty sv = (StructProperty)i;
                    var svp = ((PropListStruct)sv.data).propsList;
                    var svpr = new PropertyReader(svp);
                    string type = svpr.GetProperty<ByteProperty>("StatusValueType").enumValue;
                    ItemEntry_ConsumableAddStatusValue sve = ArkItemEntry_ConsumableAddStatusValueReader.Convert(svpr, type);
                    statusValues.Add(type, sve);
                }
            }

            //Create
            ItemEntry e = new ItemEntry
            {
                hideFromInventoryDisplay = reader.GetPropertyBool("bHideFromInventoryDisplay", false),
                useItemDurability = reader.GetPropertyBool("bUseItemDurability", false),
                isTekItem = reader.GetPropertyBool("bTekItem", false),
                allowUseWhileRiding = reader.GetPropertyBool("bAllowUseWhileRiding", false),
                name = reader.GetPropertyString("DescriptiveNameBase", null),
                description = reader.GetPropertyString("ItemDescription", null),
                spoilingTime = reader.GetPropertyFloat("SpolingTime", 0),
                baseItemWeight = reader.GetPropertyFloat("BaseItemWeight", 0),
                useCooldownTime = reader.GetPropertyFloat("MinimumUseInterval", 0),
                baseCraftingXP = reader.GetPropertyFloat("BaseCraftingXP", 0),
                baseRepairingXP = reader.GetPropertyFloat("BaseRepairingXP", 0),
                maxItemQuantity = reader.GetPropertyInt("MaxItemQuantity", 0),
                classname = DeltaDataExtractor.Program.TrimArkClassname(bp.classname),
                icon = icon,
                broken_icon = brokenIcon,
                addStatusValues = statusValues
            };
            return e;
        }
    }

    public static class ArkItemEntry_ConsumableAddStatusValueReader
    {
        public static ItemEntry_ConsumableAddStatusValue Convert(PropertyReader reader, string type)
        {
            return new ItemEntry_ConsumableAddStatusValue
            {
                baseAmountToAdd = reader.GetPropertyFloat("BaseAmountToAdd", null),
                percentOfMaxStatusValue = reader.GetPropertyBool("bPercentOfMaxStatusValue", null),
                percentOfCurrentStatusValue = reader.GetPropertyBool("bPercentOfCurrentStatusValue", null),
                useItemQuality = reader.GetPropertyBool("bUseItemQuality", null),
                addOverTime = reader.GetPropertyBool("bAddOverTime", null),
                setValue = reader.GetPropertyBool("bSetValue", null),
                setAdditionalValue = reader.GetPropertyBool("bSetAdditionalValue", null),
                addOverTimeSpeed = reader.GetPropertyFloat("AddOverTimeSpeed", null),
                itemQualityAddValueMultiplier = reader.GetPropertyFloat("ItemQualityAddValueMultiplier", null),
                statusValueType = type
            };
        }
    }
}
