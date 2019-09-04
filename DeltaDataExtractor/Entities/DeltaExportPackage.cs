using DeltaDataExtractor.OutputEntities;
using DeltaDataExtractor.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UassetToolkit;

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
        public Stream Run(out string hash)
        {
            //Get pathnames that match this
            List<ArkAsset>[] files = DiscoveryService.DiscoverFiles(installation, this);

            //Create a cache block
            UAssetCacheBlock cache = new UAssetCacheBlock();

            //Import items
            var items = ItemExtractorService.ExtractItems(cache, files[(int)ArkAssetType.InventoryItem], patch);

            //Import dinos
            //var dinos = DinoExtractorService.ExtractDinos(cache, files[(int)ArkAssetType.Dino]);

            //Return the package
            return CompilePackage(new Dictionary<string, object>
            {
                {"items.json", items }
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
                    CompilePackageHelper(archive, o.Key, o.Value, dataStream);
                }

                //Rewind and SHA-1
                dataStream.Position = 0;
                using (SHA1Managed sha1 = new SHA1Managed())
                {
                    byte[] hashBytes = sha1.ComputeHash(dataStream);
                    hash = string.Concat(hashBytes.Select(b => b.ToString("x2")));
                }
            }


            //Add the metadata
            CompilePackageHelper(archive, "metadata.json", new DeltaExportPackageMetadata
            {
                id = id,
                name = name,
                patch_tag = patch.tag,
                time = patch.time,
                sha1 = hash
            });

            //Close the archive and rewind the stream
            archive.Dispose();
            stream.Position = 0;

            return stream;
        }

        private void CompilePackageHelper(ZipArchive zip, string name, object data, MemoryStream totalOutput = null)
        {
            //Convert to JSON and get bytes
            byte[] payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));

            //Create an entry and begin writing
            ZipArchiveEntry entry = zip.CreateEntry(name);
            using (var stream = entry.Open())
                stream.Write(payload, 0, payload.Length);
            if(totalOutput != null)
                totalOutput.Write(payload, 0, payload.Length);
        }
    }
}
