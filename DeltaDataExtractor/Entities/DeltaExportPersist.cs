using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaDataExtractor.Entities
{
    /// <summary>
    /// Persistent data that contains what we've uploaded and where it is.
    /// </summary>
    public class DeltaExportPersist
    {
        /// <summary>
        /// Contains data about assets we've already exported so we don't export them again.
        /// </summary>
        public List<DeltaExportBranchExternalAsset> external_assets = new List<DeltaExportBranchExternalAsset>();
    }

    /// <summary>
    /// Contains data about assets we've exported
    /// </summary>
    public class DeltaExportBranchExternalAsset
    {
        public string url_hires;
        public string url_lores;
        public string patch;
        public string sha1; //Sha1 of the original file before it is converted.
        public string name; //The game pathname. Starts with /Game/
        public DateTime time;
    }
}
