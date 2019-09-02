using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DeltaDataExtractor.Entities
{
    /// <summary>
    /// Like an ArkNamespace, but to an asset instead
    /// </summary>
    public class ArkAsset
    {
        /// <summary>
        /// The namespace we belong to
        /// </summary>
        public ArkNamespace parent;

        /// <summary>
        /// File info for this object.
        /// </summary>
        public FileInfo info;

        /// <summary>
        /// The name of this, not including the extension (I.E. .uasset)
        /// </summary>
        public string name;

        /// <summary>
        /// The name used by the game. Starts with /Game/
        /// </summary>
        public string fullName;

        /// <summary>
        /// File extension, such as .uasset
        /// </summary>
        public string extension;

        /// <summary>
        /// The filesystem filename
        /// </summary>
        public string filename;

        /// <summary>
        /// The parent installation
        /// </summary>
        public ArkInstall installation;

        public static ArkAsset GetAssetFromGame(string name, ArkInstall install)
        {
            //Verify that we actually match a correct kind of path
            if (!name.StartsWith("/Game/"))
                throw new Exception("This is not a game namespace path.");

            //Add the folder names. We're going to assume that this is a .uasset file
            string path = install.contentFolder + name.Substring("/Game/".Length) + ".uasset";

            //Ensure this file exists
            if (!File.Exists(path))
                throw new Exception("Could not find ArkAsset from game path. Is it not a uasset file?");

            //Now, we'll get it as usualy
            return GetAssetFromFolderPath(path, install);
        }

        /// <summary>
        /// Opens the file
        /// </summary>
        /// <returns></returns>
        public Stream OpenStream()
        {
            return new FileStream(filename, FileMode.Open, FileAccess.Read);
        }

        private readonly List<ArkAssetType> UNKNOWN_TYPE = new List<ArkAssetType>() { ArkAssetType.Unknown };

        /// <summary>
        /// Does a binary search to determine the type. Sometimes catches files that don't match, do a slow check later. Thanks to coldino for telling me about this method https://discordapp.com/channels/@me/603687675848818719/603703500077269025
        /// </summary>
        public List<ArkAssetType> QuickGuessType()
        {
            //Since we over-find items, we return all types that it could be
            List<ArkAssetType> output = new List<ArkAssetType>();

            //Open buffer to check in
            byte[] buffer = new byte[2048*2];

            //Create a dict of what we're searching for. The indexes here map to the enum ArkAssetType
            byte[][] queries = new byte[][]
            {
                Encoding.ASCII.GetBytes("ShooterCharacterMovement"),
                Encoding.ASCII.GetBytes("StructureMesh"),
                Encoding.ASCII.GetBytes("DescriptiveNameBase")
            };

            //Open stream
            Stream s = OpenStream();

            //We're going to try to read the uasset name table. Get it's data from the file header
            s.Position = 28; //This puts us before a string
            int strlen = ByteReader.ReadInt32(s); //Skip a string
            if(strlen > 100 || strlen < 0)
                return UNKNOWN_TYPE; //Unexpected. Assume this is not a uasset file.
            s.Position += strlen;
            s.Position += 4;
            int len = ByteReader.ReadInt32(s);
            int pos = ByteReader.ReadInt32(s);

            //Verify the length and position
            if (pos + len > s.Length)
                return UNKNOWN_TYPE;

            //Now, we'll read all of the name table entries and see if they match one of our names
            s.Position = pos;
            for (int i = 0; i<len; i++)
            {
                //Read the string length
                strlen = ByteReader.ReadInt32(s);

                //Because this includes the null terminator, we get the real size
                int realStrLen = strlen - 1;

                //Verify
                if (realStrLen < 0 || strlen > 300)
                    return UNKNOWN_TYPE;

                //Check if the string length matches any of our queries
                bool hasMatchingLength = false;
                for(int test = 0; test<queries.Length; test+=1)
                {
                    //Check length before loading in the string
                    if (queries[test].Length != realStrLen)
                        continue;

                    hasMatchingLength = true;
                }

                //If we have any matching lengths, read them. Else, we'll just simply skip
                if(hasMatchingLength)
                {
                    //Read in the string
                    byte[] name = new byte[realStrLen];
                    s.Read(name, 0, name.Length);
                    s.Position += 1; //Skip the null terminator

                    //Loop through tests
                    for(int test = 0; test<queries.Length; test++)
                    {
                        //Check length before loading in the string
                        if (queries[test].Length != realStrLen)
                            continue;

                        //Compare
                        bool ok = true;
                        for (int j = 0; j < realStrLen; j++)
                        {
                            if (name[j] != queries[test][j])
                                ok = false;
                        }
                        if (ok && !output.Contains((ArkAssetType)test))
                            output.Add((ArkAssetType)test);
                    }
                } else
                {
                    //We'll skip
                    s.Position += strlen;
                }
            }

            //If we found results, return them. Else, return unknown
            if (output.Count == 0)
                return UNKNOWN_TYPE;
            else
                return output;
        }

        /// <summary>
        /// Gets the asset from a filesystem folder path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static ArkAsset GetAssetFromFolderPath(string path, ArkInstall install)
        {
            //Get the fileinfo
            FileInfo info = new FileInfo(path);

            //Get the parent namespace
            ArkNamespace parent = ArkNamespace.GetNamespaceFromFolderPath(info.Directory.FullName, install);

            //Create object
            ArkAsset a = new ArkAsset
            {
                info = info,
                filename = path,
                parent = parent,
                installation = install
            };

            //Get the name without the extension
            if(info.Name.Contains("."))
            {
                a.name = info.Name.Substring(0, info.Name.LastIndexOf('.'));
                a.extension = info.Name.Substring(a.name.Length + 1);
            } else
            {
                a.name = info.Name;
                a.extension = "";
            }

            //Set name
            a.fullName = parent.fullName + a.name;

            return a;
        }
    }
}
