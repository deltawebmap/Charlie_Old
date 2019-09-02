using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DeltaDataExtractor.Entities
{
    /// <summary>
    /// Looks like a folder to us. Represents a folder in the namespace.
    /// </summary>
    public class ArkNamespace
    {
        /// <summary>
        /// The directory info of this.
        /// </summary>
        public DirectoryInfo dir;

        /// <summary>
        /// The name of this namespace.
        /// </summary>
        public string name;

        /// <summary>
        /// Returns the root names, 0th element being the farthest root. Does not include us and only contains the name
        /// </summary>
        public string[] roots;
        
        /// <summary>
        /// The full namespaced name that ARK will use. Starts with /Game/
        /// </summary>
        public string fullName;

        /// <summary>
        /// The filesystem pathname to this.
        /// </summary>
        public string pathname;

        /// <summary>
        /// The root install
        /// </summary>
        public ArkInstall installation;

        /// <summary>
        /// Returns the parent namespace.
        /// </summary>
        /// <returns></returns>
        public ArkNamespace GetParent()
        {
            //Get the parent folder and check if it is the install folder
            DirectoryInfo parentDir = dir.Parent;
            ArkNamespace parentNamespace;
            if (parentDir.FullName == installation.dir.FullName)
                parentNamespace = null;
            else
                parentNamespace = GetNamespaceFromFolderPath(parentDir.FullName, installation);
            return parentNamespace;
        }

        /// <summary>
        /// Returns children namespaces
        /// </summary>
        /// <returns></returns>
        public List<ArkNamespace> GetChildren()
        {
            List<ArkNamespace> children = new List<ArkNamespace>();
            string[] paths = Directory.GetDirectories(pathname);
            foreach (var p in paths)
                children.Add(GetNamespaceFromFolderPath(p, installation));
            return children;
        }

        /// <summary>
        /// Returns children assets
        /// </summary>
        /// <returns></returns>
        public List<ArkAsset> GetAssetChildren()
        {
            List<ArkAsset> children = new List<ArkAsset>();
            string[] paths = Directory.GetFiles(pathname);
            foreach (var p in paths)
                children.Add(ArkAsset.GetAssetFromFolderPath(p, installation));
            return children;
        }

        /// <summary>
        /// Returns a named child namespace
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ArkNamespace GetChildByName(string name)
        {
            //Get the pathname
            string path = pathname + name + "/";

            //Make sure this exists
            if (!Directory.Exists(path))
                throw new Exception("Could not get child namespace because it did not exist!");

            //Get
            return GetNamespaceFromFolderPath(path, installation);
        }

        /// <summary>
        /// Returns a named child asset. Does NOT include the extension and assumes it is a usasset
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ArkAsset GetAssetChildByName(string name)
        {
            //Get the pathname
            string path = pathname + name + ".uasset";

            //Make sure this exists
            if (!File.Exists(path))
                throw new Exception("Could not get child file because it did not exist!");

            //Get
            return ArkAsset.GetAssetFromFolderPath(path, installation);
        }

        /// <summary>
        /// Gets a namespace from a game path. Will start with /Game/
        /// </summary>
        /// <param name="name"></param>
        /// <param name="install"></param>
        /// <returns></returns>
        public static ArkNamespace GetNamespaceFromGame(string name, ArkInstall install)
        {
            //Verify that we actually match a correct kind of path
            if (!name.StartsWith("/Game/"))
                throw new Exception("This is not a game namespace path.");

            //Verify that this is not an asset
            if (!name.EndsWith('/'))
                throw new Exception("This is an asset, not a namespaced folder. Use ArkAsset instead.");

            //Add the folder names
            string path = install.contentFolder + name.Substring("/Game/".Length);

            //Now, we'll get it as usualy
            return GetNamespaceFromFolderPath(path, install);
        }

        /// <summary>
        /// Gets a namespace from a folder path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="install"></param>
        /// <returns></returns>
        public static ArkNamespace GetNamespaceFromFolderPath(string path, ArkInstall install)
        {
            //Get the directory info
            DirectoryInfo info = new DirectoryInfo(path);

            //Find root names
            DirectoryInfo lastParent = info.Parent;
            List<string> rootNames = new List<string>();
            while(lastParent.FullName != install.dir.FullName)
            {
                rootNames.Add(lastParent.Name);
                lastParent = lastParent.Parent;
            }
            rootNames.Reverse(); //Reverse so that we match the direction we said

            //Create the full name
            string fullName = "/Game/";
            foreach (var s in rootNames)
                fullName += s + "/";
            fullName += info.Name + "/";

            //Make data
            ArkNamespace n = new ArkNamespace
            {
                dir = info,
                name = info.Name,
                installation = install,
                roots = rootNames.ToArray(),
                fullName = fullName,
                pathname = path
            };

            return n;
        }
    }
}
