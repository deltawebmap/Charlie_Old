using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DeltaDataExtractor.Entities.FileManager
{
    public class SFTPAssetManager : AssetManager
    {
        private SftpClient client;

        public override void Connect()
        {
            //Get enviornment
            var env = Program.config.GetProfile();
            
            //Establish a connection
            var connectionInfo = new ConnectionInfo(env.user_server, env.user_name, new PasswordAuthenticationMethod(env.user_name, env.user_password));
            client = new SftpClient(connectionInfo);
            client.Connect();
        }

        public override void Disconnect()
        {
            client.Disconnect();
        }

        public override void Upload(string pathname, Stream s)
        {
            client.UploadFile(s, pathname, true);
        }
    }
}
