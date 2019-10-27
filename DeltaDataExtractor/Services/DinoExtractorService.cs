using ArkImportTools.OutputEntities;
using DeltaDataExtractor.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using UassetToolkit;
using LibDeltaSystem.Entities.ArkEntries.Dinosaur;

namespace DeltaDataExtractor.Services
{
    public static class DinoExtractorService
    {
        public static List<DinosaurEntry> ExtractDinos(UAssetCacheBlock cache, List<ArkAsset> assets, DeltaExportPatch patch, PropertyReader primalDataReader, Dictionary<string, PropertyReader> dinoEntries)
        {
            //Loop through assets and import them
            List<DinosaurEntry> dinos = new List<DinosaurEntry>();
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
                    DinosaurEntry entry = ArkDinoEntryConverter.Convert(f, cache, patch, primalDataReader, dinoEntries);
                    dinos.Add(entry);
                } catch (Exception ex)
                {
                    continue;
                }
            }
            return dinos;
        }
    }
}
