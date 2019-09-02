using DeltaDataExtractor.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DeltaDataExtractor.Services
{
    /// <summary>
    /// Seeks for files
    /// </summary>
    public static class DiscoveryService
    {
        public static List<ArkAsset>[] DiscoverFiles(ArkInstall install, DeltaExportPackage package)
        {
            //Create exclude regexes
            Regex[] excludes = new Regex[Program.config.exclude_regex.Length];
            for(var i = 0; i<excludes.Length; i+=1)
            {
                excludes[i] = new Regex(Program.config.exclude_regex[i]);
            }

            //Create output
            List<ArkAsset>[] assets = new List<ArkAsset>[3];
            for (int i = 0; i < assets.Length; i++)
                assets[i] = new List<ArkAsset>();

            //Loop through dirs
            var dirs = install.GetChildren();
            foreach (var d in dirs)
                DiscoverNamespace(d, excludes, assets, package);

            //Write done
            Log.WriteSuccess("Discovery", $"Seek finished. Found {assets[0].Count} dinos, {assets[1].Count} structures, {assets[2].Count} items");
            return assets;
        }

        private static void DiscoverNamespace(ArkNamespace name, Regex[] excludes, List<ArkAsset>[] assets, DeltaExportPackage package)
        {
            //Check if this namespace is permitted to be used by this package
            if (!package.CheckIfNameIsInPackage(name))
                return;

            //Check all files inside of this namespace
            var files = name.GetAssetChildren();
            foreach(var f in files)
            {
                //Skip if we match any of the exclude regexes
                bool ok = true;
                for(var i = 0; i<excludes.Length; i++)
                {
                    if(excludes[i].IsMatch(f.fullName))
                    {
                        ok = false;
                    }
                }

                //Stop if we failed
                if (!ok)
                    continue;

                //Go!
                List<ArkAssetType> types = f.QuickGuessType();
                foreach(var t in types)
                {
                    int typeInt = (int)t;
                    if (typeInt != -1)
                        assets[typeInt].Add(f);
                }
            }

            //Loop through dirs
            var dirs = name.GetChildren();
            foreach (var d in dirs)
                DiscoverNamespace(d, excludes, assets, package);
        }
    }
}
