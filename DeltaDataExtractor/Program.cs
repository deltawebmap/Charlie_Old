using DeltaDataExtractor.Entities;
using DeltaDataExtractor.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UassetToolkit;

namespace DeltaDataExtractor
{
    class Program
    {
        public static ExtractorConfig config;
        public static Stream log;

        static void Main(string[] args)
        {
            //Read config
            config = JsonConvert.DeserializeObject<ExtractorConfig>(File.ReadAllText("config.json"));

            //Generate a revision tag and make it's folder structure
            log = new FileStream(config.GetProfile().charlie_log, FileMode.Create);

            //Do some logging
            Log.WriteHeader("Data created " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + " LOCAL");
            Log.WriteHeader("Deploying to "+config.enviornment);
            Log.WriteHeader("(C) RomanPort 2019");

            //Init the install
            ArkInstall install = ArkInstall.GetInstall(config.install_path);

            //Create a patch and run
            DeltaExportPatch patch = new DeltaExportPatch(install);
            patch.Go();

            //Finish
            log.Flush();
            log.Close();
            Console.ReadLine();
        }
    }
}
