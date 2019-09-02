using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DeltaDataExtractor.Entities
{
    /// <summary>
    /// Holds data about the ARK installation and assets and namespace.
    /// </summary>
    public class ArkInstall
    {
        /// <summary>
        /// The root content folder
        /// </summary>
        public string contentFolder;

        /// <summary>
        /// The directory info of the root.
        /// </summary>
        public DirectoryInfo dir;

        /// <summary>
        /// Returns children namespaces
        /// </summary>
        /// <returns></returns>
        public List<ArkNamespace> GetChildren()
        {
            List<ArkNamespace> children = new List<ArkNamespace>();
            string[] paths = Directory.GetDirectories(contentFolder);
            foreach (var p in paths)
                children.Add(ArkNamespace.GetNamespaceFromFolderPath(p, this));
            return children;
        }

        /// <summary>
        /// Gets the install base
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static ArkInstall GetInstall(string path)
        {
            return new ArkInstall
            {
                contentFolder = path.TrimEnd('\\').TrimEnd('/').Replace('\\', '/')+"/",
                dir = new DirectoryInfo(path)
            };
        }
    }
}
