using ArkImportTools;
using DeltaDataExtractor.Entities.FileManager;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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

            //Run base game
            DeltaExportPackage basePack = new DeltaExportPackage(this, installation, "ARK: Survival Evolved", "base", false);
            RunOne(basePack);
            
            //Run mods
            foreach(string id in Program.config.mods)
            {
                //Create a package for this and compute it
                DeltaExportPackage pack = new DeltaExportPackage(this, installation, "Test Mod", id, true);
                RunOne(pack);
            }

            //Process images
            ImageTool.ProcessImages(new List<string>(), this);

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
        private void RunOne(DeltaExportPackage pack)
        {
            //Run and obtain a stream
            Stream data = pack.Run();

            //Now, we'll upload it to the server
            Log.WriteInfo("DeltaExportPatch Run", "Uploading to server...");
            var profile = Program.config.GetProfile();
            asset_manager.Upload(profile.upload_packages + tag + "-" + pack.id + ".pdp", data);
            Log.WriteSuccess("DeltaExportPatch Run", "Uploaded " + data.Length + " bytes as a primal data package.");
        }

    }
}
