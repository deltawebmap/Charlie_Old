using ArkImportTools.OutputEntities;
using DeltaDataExtractor.Entities;
using LibDeltaSystem.Entities.ArkEntries.Dinosaur;
using System;
using System.Collections.Generic;
using System.Text;
using UassetToolkit;

namespace DeltaDataExtractor.Services
{
    public static class ItemExtractorService
    {
        public static List<ItemEntry> ExtractItems(UAssetCacheBlock cache, List<ArkAsset> assets, DeltaExportPatch patch)
        {
            //Loop through all assets
            List<ItemEntry> items = new List<ItemEntry>();
            foreach(var a in assets)
            {
                //Open 
                DateTime start = DateTime.UtcNow;
                UAssetFileBlueprint bp;
                try
                {
                    bp = UAssetFileBlueprint.OpenFile(a.filename, false, a.name, a.installation.contentFolder);
                }
                catch (Exception ex)
                {
                    Log.WriteError("Item Extractor", "Failed to open file " + a.fullName + ": " + ex.Message + ex.StackTrace);
                    continue;
                }

                //Decode
                try
                {
                    ItemEntry entry = ArkItemEntryReader.ConvertEntry(bp, cache, patch);
                    items.Add(entry);
                    TimeSpan time = DateTime.UtcNow - start;
                    Log.WriteInfo("Item Extractor", "Successfully opened and converted " + a.fullName +" in "+Math.Round(time.TotalMilliseconds) +"ms");
                }
                catch (Exception ex)
                {
                    Log.WriteError("Item Extractor", "Failed to import " + a.fullName+": "+ex.Message + ex.StackTrace);
                    continue;
                }
            }

            Log.WriteSuccess("Item Extractor", "Extracted " + items.Count + "/" + assets.Count + " items.");
            return items;
        }
    }
}
