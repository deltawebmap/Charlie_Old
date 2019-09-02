using ArkImportTools.OutputEntities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UassetToolkit;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;
using SixLabors.ImageSharp.Formats.Png;
using DeltaDataExtractor;
using DeltaDataExtractor.Entities;
using System.Security.Cryptography;
using System.Linq;

namespace ArkImportTools
{
    public static class ImageTool
    {
        public const string FORMAT_TYPE = ".png";

        public static ArkImage QueueImage(ClassnamePathnamePair name, ImageModifications mods, DeltaExportPatch patch)
        {
            return QueueImage(name.classname, name.pathname, mods, patch);
        }

        public static string GetAssetsUrl()
        {
            return Program.config.GetProfile().upload_url_base;
        }

        public static ArkImage QueueImage(string classname, string pathname, ImageModifications mods, DeltaExportPatch patch)
        {
            //SHA1 this file for version control
            byte[] hashBytes = new SHA1Managed().ComputeHash(File.ReadAllBytes(pathname));
            string hash = string.Concat(hashBytes.Select(b => b.ToString("x2")));

            //Get the game pathname
            ArkAsset asset = ArkAsset.GetAssetFromFolderPath(pathname, patch.installation);
            string namespacedName = asset.fullName;

            //Check for matching file pathnames and versions
            var matches = patch.persist.external_assets.Where(x => x.sha1 == hash && x.name == namespacedName).ToArray();

            //Create or get
            string hiId;
            string loId;
            if(matches.Length == 1)
            {
                //We'll use this existing image
                hiId = matches[0].url_hires;
                loId = matches[0].url_lores;
            } else
            {
                //We'll generate this

                //Generate IDs for the high res image and thumbnail
                hiId = GenerateUniqueImageID(patch);
                loId = GenerateUniqueImageID(patch);

                //Now, create an object and add it to a queue
                patch.queued_images.Add(new QueuedImage
                {
                    classname = classname,
                    pathname = pathname,
                    hiId = hiId,
                    loId = loId,
                    mods = mods,
                    sha1 = hash,
                    name = namespacedName
                });
            }

            //Now, create an ArkImage
            ArkImage r = new ArkImage
            {
                image_thumb_url = GetAssetsUrl()+ loId+FORMAT_TYPE,
                image_url = GetAssetsUrl()+ hiId+FORMAT_TYPE
            };

            return r;
        }

        public static void ProcessImages(List<string> readErrors, DeltaExportPatch patch)
        {
            //Clean up any old and bad paths
            Console.WriteLine("Cleaning up old image conversions...");
            if(Directory.Exists("./Lib/UModel/in_temp/"))
                Directory.Delete("./Lib/UModel/in_temp/", true);
            if (Directory.Exists("./Lib/UModel/out_temp/"))
                Directory.Delete("./Lib/UModel/out_temp/", true);

            //Make structre
            Directory.CreateDirectory("./Lib/UModel/in_temp/");
            Directory.CreateDirectory("./Lib/UModel/out_temp/");

            //Get the queue
            var queue = patch.queued_images;

            //First, we copy all packages to a temporary path with their index
            Console.WriteLine($"Now copying {queue.Count} images...");
            for (int i = 0; i<queue.Count; i++)
            {
                string source = queue[i].pathname;
                File.Copy(source, $"./Lib/UModel/in_temp/{i}.uasset");
            }

            //Now, run the conversion
            Console.WriteLine("Now converting images using UModel...");
            Process p = Process.Start(new ProcessStartInfo
            {
                Arguments = "",
                FileName = "go.bat",
                WorkingDirectory = "Lib\\UModel\\",
                UseShellExecute = true
            });
            p.WaitForExit();

            //Now, load and process these images
            int ok = 0;
            Console.WriteLine($"Now processing {queue.Count} images...");
            for(int i = 0; i<queue.Count; i+=1)
            {
                QueuedImage q = queue[i];

                try
                {
                    //Get the directory. It's a little janky, as files are stored in subdirs
                    string[] results = Directory.GetFiles($"./Lib/UModel/out_temp/{i}/");
                    if (results.Length != 1)
                        throw new Exception("None or too many results found for image.");

                    //Open FileStream on this
                    using (FileStream imgStream = new FileStream(results[0], FileMode.Open, FileAccess.Read))
                    {
                        //Now, begin reading the TGA data https://en.wikipedia.org/wiki/Truevision_TGA
                        IOMemoryStream imgReader = new IOMemoryStream(imgStream, true);
                        imgReader.position += 3 + 5; //Skip intro, it will always be known
                        imgReader.ReadShort(); //Will always be 0
                        imgReader.ReadShort(); //Will aways be 0
                        short width = imgReader.ReadShort();
                        short height = imgReader.ReadShort();
                        byte colorDepth = imgReader.ReadByte();
                        imgReader.ReadByte();

                        //Now, we can begin reading image data
                        //This appears to be bugged for non-square images right now.
                        using (Image<Rgba32> img = new Image<Rgba32>(width, height))
                        {
                            //Read file
                            byte[] channels;
                            for (int y = 0; y < height; y++)
                            {
                                for (int x = 0; x < width; x++)
                                {
                                    if (colorDepth == 32)
                                    {
                                        //Read four channels
                                        channels = imgReader.ReadBytes(4);

                                        //Set pixel
                                        img[x, width - y - 1] = new Rgba32(channels[2], channels[1], channels[0], channels[3]);
                                    }
                                    else if (colorDepth == 24)
                                    {
                                        //Read three channels
                                        channels = imgReader.ReadBytes(3);

                                        //Set pixel
                                        img[x, width - y - 1] = new Rgba32(channels[2], channels[1], channels[0]);
                                    }
                                }
                            }

                            //Apply mods
                            if (q.mods == ImageModifications.White)
                                ApplyWhiteMod(img);

                            //Save original image
                            using (MemoryStream ms = new MemoryStream())
                            {
                                img.SaveAsPng(ms);
                                ms.Position = 0;
                                patch.asset_manager.Upload(Program.config.GetProfile().upload_images + q.hiId + FORMAT_TYPE, ms);
                            }

                            //Now, downscale
                            img.Mutate(x => x.Resize(64, 64));

                            //Save thumbnail
                            using (MemoryStream ms = new MemoryStream())
                            {
                                img.SaveAsPng(ms, new PngEncoder
                                {
                                    CompressionLevel = 9
                                });
                                ms.Position = 0;
                                patch.asset_manager.Upload(Program.config.GetProfile().upload_images + q.loId + FORMAT_TYPE, ms);
                            }

                            //Now, add to persistent storage
                            patch.persist.external_assets.Add(new DeltaExportBranchExternalAsset
                            {
                                name = q.name,
                                patch = patch.tag,
                                sha1 = q.sha1,
                                time = DateTime.UtcNow,
                                url_hires = GetAssetsUrl() + q.hiId + FORMAT_TYPE,
                                url_lores = GetAssetsUrl() + q.loId + FORMAT_TYPE
                            });

                            ok++;
                        }
                    }
                } catch (Exception ex)
                {
                    Console.WriteLine($"Failed to process image {q.classname} with error {ex.Message}");
                    readErrors.Add($"Failed to process image {q.classname} with error {ex.Message} {ex.StackTrace}");
                }
            }
            Log.WriteSuccess("ImageTool", $"Processed and uploading {ok}/{queue.Count} images.");
            queue.Clear();

            //Clean up any old and bad paths
            Console.WriteLine("Cleaning up...");
            if (Directory.Exists("./Lib/UModel/in_temp/"))
                Directory.Delete("./Lib/UModel/in_temp/", true);
            if (Directory.Exists("./Lib/UModel/out_temp/"))
                Directory.Delete("./Lib/UModel/out_temp/", true);
        }

        private static Random rand = new Random();

        private static string GenerateUniqueImageID(DeltaExportPatch patch)
        {
            string id = GenerateUnsafeImageID();
            return id;
        }

        private static string GenerateUnsafeImageID()
        {
            //Generate an ID, it will not be guarenteed to be unique
            char[] p = "1234567890ABCDEF".ToCharArray();
            char[] output = new char[24];
            for (int i = 0; i < 24; i++)
                output[i] = p[rand.Next(0, p.Length)];
            return new string(output);
        }

        public static string GenerateID(int length)
        {
            //Generate an ID, it will not be guarenteed to be unique
            char[] p = "1234567890ABCDEF".ToCharArray();
            char[] output = new char[length];
            for (int i = 0; i < length; i++)
                output[i] = p[rand.Next(0, p.Length)];
            return new string(output);
        }

        static void ApplyWhiteMod(Image<Rgba32> img)
        {
            //Set all pixels to white, but keep the alpha
            for(int x = 0; x<img.Width; x++)
            {
                for(int y = 0; y<img.Height; y++)
                {
                    Rgba32 v = img[x, y];
                    img[x, y] = new Rgba32(255, 255, 255, v.A);
                }
            }
        }

        public class QueuedImage
        {
            public string loId;
            public string hiId;
            public string classname;
            public string pathname;
            public ImageModifications mods;
            public string sha1;
            public string name; //The namespaced name beginning with /Game/
        }

        public enum ImageModifications
        {
            None,
            White
        }
    }
}
