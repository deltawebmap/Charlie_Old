﻿using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaDataExtractor.OutputEntities
{
    public class DeltaExportPackageMetadata
    {
        public DateTime time;
        public string id;
        public string name;
        public string patch_tag;
        public string sha1; //SHA-1 of contents
    }
}
