using DeltaDataExtractor.OutputEntities;
using DeltaDataExtractor.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UassetToolkit;
using UassetToolkit.UPropertyTypes;

namespace DeltaDataExtractor.Entities
{
    /// <summary>
    /// The package that is actually exported. May contain the base game or a mod.
    /// </summary>
    public class DeltaExportPackage
    {
        /// <summary>
        /// If false, we use the base game's files.
        /// </summary>
        public bool isMod;

        /// <summary>
        /// The name of this package
        /// </summary>
        public string name;

        /// <summary>
        /// The unique ID for this package. Will also be the mod folder read if isMod is true.
        /// </summary>
        public string id;

        /// <summary>
        /// The Ark installation dir to read from.
        /// </summary>
        public ArkInstall installation;

        /// <summary>
        /// The parent patch of this package
        /// </summary>
        public DeltaExportPatch patch;

        /// <summary>
        /// Creates a package object.
        /// </summary>
        /// <param name="patch"></param>
        /// <param name="install"></param>
        public DeltaExportPackage(DeltaExportPatch patch, ArkInstall install, string name, string id, bool isMod)
        {
            installation = install;
            this.patch = patch;
            this.name = name;
            this.id = id;
            this.isMod = isMod;
        }

        /// <summary>
        /// Returns true if this is a namespace that should be added to this.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool CheckIfNameIsInPackage(ArkNamespace name)
        {
            //This behaves very differently if this is a mod or not
            if(isMod)
            {
                //Only namespaces inside of the Mods/{id}/ folder are OK
                return name.fullName.StartsWith("/Game/Mods/" + id + "/");
            } else
            {
                //We allow everything BUT a mod that isn't a system mod
                if(name.fullName.StartsWith("/Game/Mods/"))
                {
                    //We'll have to check if this is a system mod
                    string modId = name.fullName.Substring("/Game/Mods/".Length);
                    if (modId.Contains('/'))
                        modId = modId.Substring(0, modId.IndexOf('/'));
                    if (modId.Length == 0)
                        return true; //We'll scan subdirs
                    return Program.config.system_mods.Contains(modId);
                } else
                {
                    //This will always be permitted.
                    return true;
                }
            }
        }

        /// <summary>
        /// Extracts data and returns the package
        /// </summary>
        public Stream Run(UAssetCacheBlock cache, out string hash)
        {
            //Get pathnames that match this
            List<ArkAsset>[] files = DiscoveryService.DiscoverFiles(installation, this);

            //Load mod info if this is a mod. TODO

            //Load Primal Data for mod
            string primalDataPathname = "/Game/PrimalEarth/CoreBlueprints/PrimalGameData_BP";
            ArkAsset primalDataNs = ArkAsset.GetAssetFromGame(primalDataPathname, installation);
            UAssetFileBlueprint primalData = UAssetFileBlueprint.OpenFile(primalDataNs.filename, false, primalDataNs.name, this.installation.contentFolder);
            PropertyReader primalDataReader = new PropertyReader(primalData.GetFullProperties(cache));

            //Load all dino entries
            var dinoEntriesArray = primalDataReader.GetProperty<ArrayProperty>("DinoEntries");
            Dictionary<string, PropertyReader> dinoEntries = new Dictionary<string, PropertyReader>(); //Dino entries mapped by tag name
            foreach(var i in dinoEntriesArray.props)
            {
                var ii = ((ObjectProperty)i).GetReferencedFileBlueprint();
                var iir = new PropertyReader(ii.GetFullProperties(cache));
                string tag = iir.GetPropertyStringOrName("DinoNameTag");
                if (!dinoEntries.ContainsKey(tag))
                    dinoEntries.Add(tag, iir);
                else
                    dinoEntries[tag] = iir;
            }

            //Import dinos
            var dinos = DinoExtractorService.ExtractDinos(cache, files[(int)ArkAssetType.Dino], patch, primalDataReader, dinoEntries);

            //Import items
            var items = ItemExtractorService.ExtractItems(cache, files[(int)ArkAssetType.InventoryItem], patch);

            Log.WriteSuccess("Export-Package-Run", $"Package {name}: Processed {dinos.Count} dinos, {items.Count} items");

            //Return the package
            return CompilePackage(new Dictionary<string, object>
            {
                {"items.bson", items },
                {"dinos.bson", dinos }
            }, out hash);
        }

        /// <summary>
        /// Produces a ZIP file with this data
        /// </summary>
        /// <param name="payload"></param>
        private Stream CompilePackage(Dictionary<string, object> payload, out string hash)
        {
            //Create the ZIP archive
            MemoryStream stream = new MemoryStream();
            ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true);

            //Add all of our objects, then SHA-1 them
            using (MemoryStream dataStream = new MemoryStream())
            {
                //Add all
                foreach (var o in payload)
                {
                    CompilePackageHelper(archive, o.Key, o.Value);
                }
            }

            //Close the archive and rewind the stream
            archive.Dispose();

            //Rewind and SHA-1
            stream.Position = 0;
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                byte[] hashBytes = sha1.ComputeHash(stream);
                hash = string.Concat(hashBytes.Select(b => b.ToString("x2")));
            }

            //Rewind and return
            stream.Position = 0;
            return stream;
        }

        private ZipArchiveEntry CompilePackageHelper(ZipArchive zip, string name, object data)
        {
            //Create an entry and begin writing
            ZipArchiveEntry entry = zip.CreateEntry(name);
            using (var stream = entry.Open())
            {
                using (BsonWriter writer = new BsonWriter(stream))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(writer, data);
                }
            }
            return entry;
        }
    }
}
