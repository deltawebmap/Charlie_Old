using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DeltaDataExtractor.Entities.FileManager
{
    public abstract class AssetManager
    {
        public abstract void Connect();
        public abstract void Upload(string pathname, Stream s);
        public abstract void Disconnect();
    }
}
