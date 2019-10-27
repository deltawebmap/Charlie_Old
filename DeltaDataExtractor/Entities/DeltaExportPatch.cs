using ArkImportTools;
using DeltaDataExtractor.Entities.FileManager;
using DeltaDataExtractor.OutputEntities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static ArkImportTools.ImageTool;

namespace DeltaDataExtractor.Entities
{
    /// <summary>
    /// Contains a group of DeltaPackages.
    /// </summary>
    public class DeltaExportPatch
    {
        /// <summary>
        /// The unique ID of this patch
        /// </summary>
        public string tag;

        /// <summary>
        /// The start time of this patch
        /// </summary>
        public DateTime time;

        /// <summary>
        /// The ARK install to use
        /// </summary>
        public ArkInstall installation;

        /// <summary>
        /// Images that are queued to be processed when this patch is complete.
        /// </summary>
        public List<QueuedImage> queued_images;

        /// <summary>
        /// Persistent storage used for version control
        /// </summary>
        public DeltaExportPersist persist;

        /// <summary>
        /// Class to manage assets
        /// </summary>
        public AssetManager asset_manager;

        /// <summary>
        /// Creates a new branch
        /// </summary>
        /// <param name="install"></param>
        public DeltaExportPatch(ArkInstall install)
        {
            //Generate a tag
            tag = ArkImportTools.ImageTool.GenerateID(12) + "-" + Program.config.enviornment;
            Log.WriteSuccess("DeltaExportPatch", "Created a new patch with ID " + tag);
            time = DateTime.UtcNow;
            this.installation = install;
            this.queued_images = new List<QueuedImage>();
        }

        /// <summary>
        /// Creates all packages
        /// </summary>
        public void Go()
        {
            //Get enviornment we'll be using
            var env = Program.config.GetProfile();

            //Try to open the persistent storage
            if (File.Exists(env.persist_storage_path))
                persist = JsonConvert.DeserializeObject<DeltaExportPersist>(File.ReadAllText(env.persist_storage_path));
            else
                persist = new DeltaExportPersist();
            
            //Connect asset manager
            asset_manager = new SFTPAssetManager();
            asset_manager.Connect();

            //Create cache
            UassetToolkit.UAssetCacheBlock cache = new UassetToolkit.UAssetCacheBlock();

            //Run base game
            DeltaExportPackage basePack = new DeltaExportPackage(this, installation, "ARK: Survival Evolved", "base", false);
            RunOne(cache, basePack);
            
            //Run mods
            foreach(string id in Program.config.mods)
            {
                //Create a package for this and compute it
                DeltaExportPackage pack = new DeltaExportPackage(this, installation, "Test Mod", id, true);
                RunOne(cache, pack);
            }

            //Process images
            ImageTool.ProcessImages(new List<string>(), this);

            //Upload new config file
            Log.WriteSuccess("DeltaExportPackage", "Almost finished, uploading new config to server...");
            using(MemoryStream cfgStream = new MemoryStream())
            {
                //Create config file
                OutputFile outputCfg = new OutputFile
                {
                    latest_patch = tag,
                    latest_patch_time = time,
                    packages = persist.packages
                };

                //Get bytes and copy
                byte[] buf = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(outputCfg, Formatting.Indented));
                cfgStream.Write(buf, 0, buf.Length);

                //Rewind and upload
                cfgStream.Position = 0;
                asset_manager.Upload(env.upload_config, cfgStream);
            }

            //Save persistent storage
            File.WriteAllText(env.persist_storage_path, JsonConvert.SerializeObject(persist, Formatting.Indented));

            //Disconnect from the server
            asset_manager.Disconnect();

            //Done!
            Log.WriteSuccess("DeltaExportPatch", "Done creating patch " + tag + "!");
        }

        /// <summary>
        /// Computes one package
        /// </summary>
        private string RunOne(UassetToolkit.UAssetCacheBlock cache, DeltaExportPackage pack)
        {
            //Run and obtain a stream
            Stream data = pack.Run(cache, out string hash);

            //Create filename
            string filename = tag + "-" + pack.id + ".pdp";

            //Check if we already have data for this from before
            var packagePersistData = persist.packages.Where(x => x.name == pack.name).ToArray();
            string previousHash = null;
            if(packagePersistData.Length == 0)
            {
                //We do not already have data for this. We do not need to worry about version control, but we should add our own entry
                AddPackageToPersist(pack, hash, filename);
            } else if(packagePersistData.Length == 1)
            {
                //We already have data. If the previous hash is different to our current hash, we'll update it
                previousHash = packagePersistData[0].sha1;
                if(previousHash != hash)
                {
                    persist.packages.Remove(packagePersistData[0]);
                    AddPackageToPersist(pack, hash, filename);
                }
            } else
            {
                //More than one. Abort!
                throw new Exception();
            }

            //Check if this is up to date
            if(previousHash != hash)
            {
                //Now, we'll upload it to the server
                Log.WriteInfo("DeltaExportPatch Run", "Uploading to server...");
                var profile = Program.config.GetProfile();
                asset_manager.Upload(profile.upload_packages + filename, data);
                Log.WriteSuccess("DeltaExportPatch Run", "Uploaded " + data.Length + " bytes as a primal data package.");
            } else
            {
                Log.WriteInfo("DeltaExportPatch Run", "Package was already up to date: "+hash);
            }

            return hash;
        }

        private void AddPackageToPersist(DeltaExportPackage pack, string hash, string filename)
        {
            persist.packages.Add(new DeltaExportBranchPackage
            {
                name = pack.name,
                patch = tag,
                sha1 = hash,
                time = DateTime.UtcNow,
                url = Program.config.GetProfile().package_url_base + filename,
                id = pack.id
            });
        }
    }
}
