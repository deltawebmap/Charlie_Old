using DeltaDataExtractor.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using UassetToolkit;
using UassetToolkit.UPropertyTypes;
using UassetToolkit.UStructTypes;
using LibDeltaSystem.Entities.ArkEntries.Dinosaur;

namespace ArkImportTools.OutputEntities
{
    public static class ArkDinoEntryConverter
    {
        public static DinosaurEntry Convert(UAssetFileBlueprint f, UAssetCacheBlock cache, DeltaExportPatch patch, PropertyReader primalDataReader, Dictionary<string, PropertyReader> dinoEntries)
        {
            //Open reader
            PropertyReader reader = new PropertyReader(f.GetFullProperties(cache));

            //Get the dino settings
            UAssetFileBlueprint settingsFileAdult = ArkDinoFoodConverter.GetAdultFile(f, cache);
            UAssetFileBlueprint settingsFileBaby = ArkDinoFoodConverter.GetBabyFile(f, cache);

            //Get status component
            UAssetFileBlueprint statusComponent = ArkDinoEntryStatusConverter.GetFile(f, cache);
            PropertyReader statusReader = new PropertyReader(statusComponent.GetFullProperties(cache));

            //Use name tag to find entry
            string tag = reader.GetPropertyStringOrName("DinoNameTag");
            PropertyReader entry = dinoEntries[tag];

            //Now, load the material used for the dino image
            UAssetFileMaterial entryMaterial = entry.GetProperty<ObjectProperty>("DinoMaterial").GetReferencedFileMaterial();
            UAssetFileMaterial.TextureParameterValue entryMaterialTexture = entryMaterial.textureParameters[0];
            ClassnamePathnamePair entryTexture = entryMaterialTexture.prop.GetReferencedFile();

            //Read
            DinosaurEntry e = new DinosaurEntry
            {
                screen_name = reader.GetPropertyString("DescriptiveName", null),
                colorizationIntensity = reader.GetPropertyFloat("ColorizationIntensity", 1),
                babyGestationSpeed = reader.GetPropertyFloat("BabyGestationSpeed", -1),
                extraBabyGestationSpeedMultiplier = reader.GetPropertyFloat("ExtraBabyGestationSpeedMultiplier", -1),
                babyAgeSpeed = reader.GetPropertyFloat("BabyAgeSpeed", null),
                extraBabyAgeMultiplier = reader.GetPropertyFloat("ExtraBabyAgeSpeedMultiplier", -1),
                useBabyGestation = reader.GetPropertyBool("bUseBabyGestation", false),
                statusComponent = ArkDinoEntryStatusConverter.Convert(statusComponent, statusReader),
                adultFoods = ArkDinoFoodConverter.Convert(settingsFileAdult, cache),
                childFoods = ArkDinoFoodConverter.Convert(settingsFileBaby, cache),
                classname = DeltaDataExtractor.Program.TrimArkClassname(f.classname),
                icon = ImageTool.QueueImage(entryTexture, ImageTool.ImageModifications.None, patch),
            };

            //Finally, read stats
            ArkStatsRipper.DoRipStats(statusReader, e);

            return e;
        }
    }

    public static class ArkDinoEntryStatusConverter
    {
        public static UAssetFileBlueprint GetFile(UAssetFileBlueprint f, UAssetCacheBlock cache)
        {
            //Search for this by name
            GameObjectTableHead hr = null;
            UAssetFileBlueprint workingFile = f;
            while(hr == null && workingFile != null)
            {
                //Search
                foreach (var h in workingFile.gameObjectReferences)
                {
                    if (h.name.StartsWith("DinoCharacterStatusComponent_BP_"))
                        hr = h;
                }

                //Try to get the parent file
                workingFile = workingFile.GetParentBlueprint(cache);
            }
            if (hr == null)
                throw new Exception("Could not find dino status component!");

            //Open file
            string pathname = f.GetReferencedUAssetPathname(hr);
            return f.GetReferencedUAssetBlueprintFromPathname(pathname);
        }

        public static DinosaurEntryStatusComponent Convert(UAssetFileBlueprint f, PropertyReader reader)
        {
            return new DinosaurEntryStatusComponent
            {
                baseFoodConsumptionRate = reader.GetPropertyFloat("BaseFoodConsumptionRate", null),
                babyDinoConsumingFoodRateMultiplier = reader.GetPropertyFloat("BabyDinoConsumingFoodRateMultiplier", 25.5f),
                extraBabyDinoConsumingFoodRateMultiplier = reader.GetPropertyFloat("ExtraBabyDinoConsumingFoodRateMultiplier", 20),
                foodConsumptionMultiplier = reader.GetPropertyFloat("FoodConsumptionMultiplier", 1),
                tamedBaseHealthMultiplier = reader.GetPropertyFloat("TamedBaseHealthMultiplier", 1)
            };
        }
    }

    public static class ArkDinoFoodConverter
    {
        public static UAssetFileBlueprint GetAdultFile(UAssetFileBlueprint f, UAssetCacheBlock cache)
        {
            //First, try to see if it's a property
            PropertyReader r = new PropertyReader(f.GetFullProperties(cache));
            ObjectProperty p = r.GetProperty<ObjectProperty>("AdultDinoSettings");
            if(p != null)
            {
                //Get file
                return p.GetReferencedFileBlueprint();
            }

            //Get the base DinoSettingsClass property
            p = r.GetProperty<ObjectProperty>("DinoSettingsClass");
            if(p != null)
            {
                //Get file
                return p.GetReferencedFileBlueprint();
            }

            //Throw error
            throw new Exception("Dino settings class was not found.");
        }

        public static UAssetFileBlueprint GetBabyFile(UAssetFileBlueprint f, UAssetCacheBlock cache)
        {
            //First, try to see if it's a property
            PropertyReader r = new PropertyReader(f.GetFullProperties(cache));
            ObjectProperty p = r.GetProperty<ObjectProperty>("BabyDinoSettings");
            if (p != null)
            {
                //Get file
                return p.GetReferencedFileBlueprint();
            }

            //Fallback to adult settings
            return GetAdultFile(f, cache);
        }

        public static List<DinosaurEntryFood> Convert(UAssetFileBlueprint f, UAssetCacheBlock cache)
        {
            //Open reader
            PropertyReader reader = new PropertyReader(f.GetFullProperties(cache));
            List<DinosaurEntryFood> output = new List<DinosaurEntryFood>();

            //Get each
            ArrayProperty mBase = reader.GetProperty<ArrayProperty>("FoodEffectivenessMultipliers");
            ArrayProperty mExtra = reader.GetProperty<ArrayProperty>("ExtraFoodEffectivenessMultipliers");

            //Convert
            if (mBase != null)
                output.AddRange(ConvertMultiplier(f, cache, mBase));
            if (mExtra != null)
                output.AddRange(ConvertMultiplier(f, cache, mExtra));

            return output;
        }

        private static List<DinosaurEntryFood> ConvertMultiplier(UAssetFileBlueprint f, UAssetCacheBlock cache, ArrayProperty p)
        {
            //Convert each entry
            List<DinosaurEntryFood> output = new List<DinosaurEntryFood>();
            foreach (var s in p.props)
            {
                StructProperty data = (StructProperty)s;
                PropListStruct sdata = (PropListStruct)data.data;
                PropertyReader reader = new PropertyReader(sdata.propsList);
                UAssetFileBlueprint foodClass = reader.GetProperty<ObjectProperty>("FoodItemParent").GetReferencedFileBlueprint();
                DinosaurEntryFood food = new DinosaurEntryFood
                {
                    classname = foodClass.classname,
                    foodEffectivenessMultiplier = reader.GetPropertyFloat("FoodEffectivenessMultiplier", null),
                    affinityOverride = reader.GetPropertyFloat("AffinityOverride", null),
                    affinityEffectivenessMultiplier = reader.GetPropertyFloat("AffinityEffectivenessMultiplier", null),
                    foodCategory = reader.GetPropertyInt("FoodItemCategory", null),
                    priority = reader.GetPropertyFloat("UntamedFoodConsumptionPriority", null)
                };
                output.Add(food);
            }
            return output;
        }
    }
}
