using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DeltaDataExtractor
{
    public class ExtractorConfig
    {
        public string install_path;
        public string output_path;
        public string[] exclude_regex;
        public List<string> system_mods; //Mod names that are part of the base game. Mods such as Ragnork and The Center
        public List<string> mods; //Mod IDs to use
        public string enviornment; //The enviornment tag we're using such as "prod".
        public Dictionary<string, ExtractorConfigEnviornment> profiles; //Profiles, mapped to the enviornment

        public ExtractorConfigEnviornment GetProfile()
        {
            return profiles[enviornment];
        }
    }

    public class ExtractorConfigEnviornment
    {
        //Uploading
        public string user_name;
        public string user_password;
        public string user_server;

        //Locations of output
        public string upload_packages;
        public string upload_version;
        public string upload_images;

        //URL
        public string upload_url_base;

        //Misc
        public string persist_storage_path;
        public string charlie_log;
    }
}
