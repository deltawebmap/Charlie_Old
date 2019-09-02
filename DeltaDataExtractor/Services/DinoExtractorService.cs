using ArkImportTools.OutputEntities;
using DeltaDataExtractor.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using UassetToolkit;

namespace DeltaDataExtractor.Services
{
    public static class DinoExtractorService
    {
        public static List<ArkItemEntry> ExtractDinos(UAssetCacheBlock cache, List<ArkAsset> assets, DeltaExportPatch patch)
        {
            //Loop through assets and import them
            foreach(var a in assets)
            {
                //Open file
                UAssetFileBlueprint f;
                try
                {
                    f = UAssetFileBlueprint.OpenFile(a.filename, false, a.name, a.installation.contentFolder);
                } catch (Exception ex)
                {
                    continue;
                }

                //Convert file
                try
                {
                    //Create a dino entry
                    ArkDinoEntry entry = ArkDinoEntry.Convert(f, cache, patch);
                    //dinos.Add(entry);
                } catch (Exception ex)
                {
                    continue;
                }
            }
            return null;
        }
    }
}
