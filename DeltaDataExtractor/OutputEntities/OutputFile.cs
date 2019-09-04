using DeltaDataExtractor.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaDataExtractor.OutputEntities
{
    /// <summary>
    /// The file clients read to download the packages
    /// </summary>
    public class OutputFile
    {
        /// <summary>
        /// Latest patch ID
        /// </summary>
        public string latest_patch;

        /// <summary>
        /// Latest time of the last patch
        /// </summary>
        public DateTime latest_patch_time;
        
        /// <summary>
        /// The list of packages we have
        /// </summary>
        public List<DeltaExportBranchPackage> packages;
    }
}
