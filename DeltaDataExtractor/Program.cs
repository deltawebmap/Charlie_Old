using DeltaDataExtractor.Entities;
using DeltaDataExtractor.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UassetToolkit;
using LibDeltaSystem;

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
            log = new FileStream(config.GetProfile().log_path, FileMode.Create);

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

        public static string TrimArkClassname(string name)
        {
            if (name.EndsWith("_C"))
                return name.Substring(0, name.Length - 1);
            return name;
        }
    }
}
