using PCRE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace mohaaTikToMd3Helper
{
    class Program
    {
        static void Main(string[] args)
        {
            string startDirectory = args[0];
            string targetDirectory = args[1];
            string shaderDirectory = Path.Combine(startDirectory,"..","shaders");
            string shaderDirectory2 = Path.Combine(startDirectory,"..","scripts");
            List<string> shadFiles = new List<string>();
            shadFiles.AddRange(crawlDirectory(shaderDirectory));
            shadFiles.AddRange(crawlDirectory(shaderDirectory2));
            string[] files = crawlDirectory(startDirectory);
            Dictionary<string, string> tikFiles = new Dictionary<string, string>();
            Dictionary<string, TikData> parsedTiks = new Dictionary<string, TikData>();
            Dictionary<string,string> md3Files = new Dictionary<string, string>();
            Dictionary<string,string> shaderFiles = new Dictionary<string, string>();
            Dictionary<string,string> parsedShaders = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            Dictionary<string,string> processedShaders = new Dictionary<string, string>();
            foreach (string file in files)
            {
                string basename = Path.GetFileNameWithoutExtension(file);
                string extension = Path.GetExtension(file).ToLowerInvariant();
                if(extension == ".tik")
                {
                    tikFiles[basename] = file;
                    parsedTiks[file] = ParseTik(file);
                } else if(extension == ".md3")
                {
                    md3Files[basename] = file;
                } 
            }
            foreach (string file in shadFiles)
            {
                string basename = Path.GetFileNameWithoutExtension(file);
                string extension = Path.GetExtension(file).ToLowerInvariant();
                if (extension == ".shader")
                {
                    ParseShader(file,ref  parsedShaders);
                    shaderFiles[basename] = file;
                }
            }


            // Go through .tik files
            foreach(var kvp in parsedTiks)
            {
                string skelModelName = kvp.Value.skelModel;
                string skelModelBaseName = Path.GetFileNameWithoutExtension(skelModelName);
                if (md3Files.ContainsKey(skelModelBaseName))
                {
                    Console.WriteLine(md3Files[skelModelBaseName]);
                    //string relativePath = Path.GetRelativePath(startDirectory, md3Files[skelModelBaseName]);
                    string relativePath = Path.GetRelativePath(startDirectory, kvp.Key);
                    string targetPath = Path.Combine(targetDirectory, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                    byte[] moddedMD3 = ModMD3(md3Files[skelModelBaseName],kvp.Value.scale,kvp.Value.surfaceToShaderMappings,ref parsedShaders,ref processedShaders);
                    targetPath = Path.ChangeExtension(targetPath,".md3");
                    File.WriteAllBytes(targetPath,moddedMD3);
                }
            }

            StringBuilder sb = new StringBuilder();
            foreach(var kvp in processedShaders)
            {
                sb.Append("\n");
                sb.Append(kvp.Key);
                sb.Append("\n");
                sb.Append(kvp.Value);
                sb.Append("\n");
                sb.Append("\n");
            }

            File.WriteAllText("mohaaConvertShaders.shader",sb.ToString());

            sb.Clear();

            foreach (var kvp in parsedTiks)
            {
                string skelModelName = kvp.Value.skelModel;
                string skelModelBaseName = Path.GetFileNameWithoutExtension(skelModelName);
                if (md3Files.ContainsKey(skelModelBaseName))
                {
                    sb.Append($"{kvp.Key}:{kvp.Value.scale}\n");
                }
            }

            File.WriteAllText("tikScales.txt", sb.ToString());


        }

        // C# regex doesnt support ?R
        //static Regex shaderRegex = new Regex(@"(?<shaderName>[\w\d\/]+)?\s+(?<shaderBody>\{(?:[^\{\}]|(?R))*\})", RegexOptions.IgnoreCase | RegexOptions.IgnoreCase | RegexOptions.Compiled);     

        private static void ParseShader(string file, ref Dictionary<string, string> shaderData)
        {
            string shaderText = File.ReadAllText(file);
            //var otherMatches = PcreRegex.Matches(shaderText, @"(?<shaderName>[\w\d\/]+)?\s+(?<shaderBody>\{(?:[^\{\}]|(?R))*\})");
            //var otherMatches = PcreRegex.Matches(shaderText, @"(?<shaderName>[\w\d\/]+)?\s+(?:\/\/[^\n]+\s*)?(?<shaderBody>\{(?:[^\{\}]|(?R))*\})");
            var otherMatches = PcreRegex.Matches(shaderText, @"(?<shaderName>[-_\w\d\/]+)?\s+(?:\/\/[^\n]+\s*)?(?<shaderBody>\{(?:[^\{\}]|(?R))*\})");
            foreach(var match in otherMatches)
            {
                shaderData[match.Groups["shaderName"].Value] = match.Groups["shaderBody"].Value;
            }

            /*MatchCollection matches = shaderRegex.Matches(shaderText);
            foreach(Match match in matches)
            {
                shaderData[match.Groups["shaderName"].Value] = match.Groups["shaderBody"].Value;
            }*/
        }

        private static byte[] ModMD3(string md3File, float scale, Dictionary<string, string> surfaceToShaderMappings, ref Dictionary<string, string> parsedShaders, ref Dictionary<string, string> processedShaders)
        {
            byte[] data = File.ReadAllBytes(md3File);
            byte[] retVal = null;
            using (MemoryStream ms = new MemoryStream(data))
            {
                using(BinaryReader br = new BinaryReader(ms))
                {
                    using(MemoryStream msOutput = new MemoryStream())
                    {
                        using(BinaryWriter bw = new BinaryWriter(msOutput))
                        {

                            bw.Write(br.ReadBytes(4)); //IDENT (IDP3)
                            bw.Write(br.ReadBytes(4)); //VERSION
                            bw.Write(br.ReadBytes(64)); //Name
                            bw.Write(br.ReadBytes(4)); //Flags
                            bw.Write(br.ReadBytes(4)); //NumFrames
                            bw.Write(br.ReadBytes(4)); //NumTags
                            int numSurfaces = br.ReadInt32();//NumSurfaces
                            bw.Write(numSurfaces); 
                            bw.Write(br.ReadBytes(4)); //NumSkins

                            long offsetWriteOffset = bw.BaseStream.Position;

                            int offsetFrames = br.ReadInt32();
                            bw.Write(offsetFrames);
                            int offsetTags = br.ReadInt32();
                            bw.Write(offsetTags);
                            int offsetSurfaces = br.ReadInt32();
                            bw.Write(offsetSurfaces);
                            int offsetEOF = br.ReadInt32();
                            bw.Write(offsetEOF);

                            long everythingUpToSurfaces = offsetSurfaces - br.BaseStream.Position;
                            if(everythingUpToSurfaces > 0)
                            {
                                bw.Write(br.ReadBytes((int)everythingUpToSurfaces)); //NumTags
                            }

                            long totalOffsetExtra = 0;

                            // Parse/rewrite surfaces
                            for(int i=0;i< numSurfaces; i++)
                            {
                                long offsetSurfaceStart = br.BaseStream.Position;
                                long offsetSurfaceStartOut = bw.BaseStream.Position;
                                bw.Write(br.ReadBytes(4)); //IDENT (IDP3)
                                byte[] surfaceName = br.ReadBytes(64);
                                string surfaceNameString = Encoding.ASCII.GetString(surfaceName).TrimEnd('\0');
                                bool surfaceIsMapped = surfaceToShaderMappings.ContainsKey(surfaceNameString);
                                bw.Write(surfaceName);
                                bw.Write(br.ReadBytes(4)); //Flags
                                bw.Write(br.ReadBytes(4)); //numFrames
                                int numShaders = br.ReadInt32();//NumSurfaces
                                bw.Write(Math.Max(surfaceIsMapped ? 1 : 0, numShaders));
                                int numVerts = br.ReadInt32();
                                bw.Write(numVerts);
                                bw.Write(br.ReadBytes(4)); //numTriangles

                                int offsetTriangles = br.ReadInt32();
                                int offsetShaders = br.ReadInt32();
                                int offsetST = br.ReadInt32();
                                int offsetXYZNormal = br.ReadInt32();
                                int offsetEnd = br.ReadInt32();
                                long offsetSurfaceEnd = offsetSurfaceStart + offsetEnd;
                                long offsetSurfaceXYZNormals = offsetSurfaceStart + offsetXYZNormal;
                                long offsetSurfaceXYZNormalsOut = offsetSurfaceStartOut + offsetXYZNormal;

                                if (surfaceIsMapped)
                                {

                                    if (numShaders > 0)
                                    {
                                        Console.WriteLine("Weird, already had shader! Can't handle this!!");
                                        bw.Write(offsetTriangles);
                                        bw.Write(offsetShaders);
                                        bw.Write(offsetST);
                                        bw.Write(offsetXYZNormal);
                                        bw.Write(offsetEnd);
                                        for (int a = 0; a < numShaders; a++)
                                        {
                                            bw.Write(br.ReadBytes(64));
                                            bw.Write(br.ReadBytes(4));
                                        }
                                    }
                                    else
                                    {
                                        totalOffsetExtra += 64 + 4;
                                        bw.Write(offsetTriangles + 64 + 4);
                                        bw.Write(offsetShaders);
                                        bw.Write(offsetST + 64 + 4);
                                        bw.Write(offsetXYZNormal + 64 + 4);
                                        bw.Write(offsetEnd + 64 + 4);
                                        offsetSurfaceXYZNormalsOut += 64 + 4;

                                        string shaderToUseStr = surfaceToShaderMappings[surfaceNameString];

                                        if(shaderToUseStr == null || !surfaceToShaderMappings.ContainsKey(surfaceNameString))
                                        {
                                            Console.WriteLine("Surface shader mapping not found wtf");
                                        }

                                        if (shaderToUseStr.Contains('/')) // Not tested since it didn't occur.
                                        {
                                            // Fine.
                                            Console.WriteLine("Already has / no need to do shader stuff. Only collect");
                                            string shaderData = parsedShaders.ContainsKey(shaderToUseStr) ? parsedShaders[shaderToUseStr] : null;
                                            if (shaderData == null)
                                            {
                                                shaderData = @"{
	qer_editorimage " + shaderToUseStr + @"
	cull	disable
    {
        map " + shaderToUseStr + @"
    }
    {
		map $lightmap
		blendFunc filter
    }
}";
                                            }
                                            processedShaders[shaderToUseStr] = shaderData;
                                        } else
                                        {
                                            string shaderData = parsedShaders.ContainsKey(shaderToUseStr) ? parsedShaders[shaderToUseStr] : null;

                                            string baseFolder = Path.GetDirectoryName(md3File).Replace('\\', '/');
                                            string shaderToUseStrOrg = shaderToUseStr;
                                            shaderToUseStr = $"{baseFolder}/{shaderToUseStr}";
                                            if (shaderData == null)
                                            {
                                                shaderData = @"{
	qer_editorimage "+ shaderToUseStrOrg + @"
	cull	disable
    {
        map "+ shaderToUseStrOrg + @"
    }
    {
		map $lightmap
		blendFunc filter
    }
}";
                                            }
                                            processedShaders[shaderToUseStr] = shaderData;
                                        }


                                        byte[] shaderToUse = Encoding.ASCII.GetBytes(shaderToUseStr);
                                        byte[] shaderToUse64 = new byte[64];
                                        Array.Copy(shaderToUse,shaderToUse64,shaderToUse.Length);
                                        bw.Write(shaderToUse64);
                                        bw.Write((int)0); // shaderIndex. is used ingame, not important i think.
                                    }
                                } else
                                {
                                    bw.Write(offsetTriangles);
                                    bw.Write(offsetShaders);
                                    bw.Write(offsetST);
                                    bw.Write(offsetXYZNormal);
                                    bw.Write(offsetEnd);
                                }

                                long bytesLeft = offsetSurfaceEnd- br.BaseStream.Position;
                                bw.Write(br.ReadBytes((int)bytesLeft));

                                // Scale the model. (this was wrong actually, no need apparently)
                                long oldReaderPos = br.BaseStream.Position;
                                long oldWriterPos = bw.BaseStream.Position;

                                br.BaseStream.Seek(offsetSurfaceXYZNormals, SeekOrigin.Begin);
                                bw.BaseStream.Seek(offsetSurfaceXYZNormalsOut, SeekOrigin.Begin);

                                for(int v = 0;v< numVerts; v++)
                                {
                                    bw.Write( (Int16)((float)br.ReadInt16() * scale));
                                    bw.Write( (Int16)((float)br.ReadInt16() * scale));
                                    bw.Write( (Int16)((float)br.ReadInt16() * scale));
                                    bw.Write( br.ReadInt16()); // Encoded normal vector, no scaling.
                                }

                                br.BaseStream.Seek(oldReaderPos,SeekOrigin.Begin);
                                bw.BaseStream.Seek(oldWriterPos, SeekOrigin.Begin);
                            }


                            bw.BaseStream.Seek(offsetWriteOffset, SeekOrigin.Begin);
                            bw.Write(offsetFrames);
                            bw.Write(offsetTags);
                            bw.Write(offsetSurfaces);
                            bw.Write(offsetEOF+ totalOffsetExtra);

                            retVal = msOutput.ToArray();
                        }
                    }
                }
            }


            return retVal;
        }






        class TikData
        {
            public float scale;
            public string skelModel;
            public Dictionary<string, string> surfaceToShaderMappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        }

        static Regex skelModelRegex = new Regex(@"\n\s*skelModel\s+(?<skelModel>[^\n\r]+)",RegexOptions.IgnoreCase|RegexOptions.IgnoreCase|RegexOptions.Compiled);
        static Regex scaleRegex = new Regex(@"\n\s*scale\s+(?<scale>[\d\.]+)",RegexOptions.IgnoreCase|RegexOptions.IgnoreCase|RegexOptions.Compiled);
        static Regex surfaceShaderRegex = new Regex(@"\n\s*surface\s+(?<surface>[^\s]+)\s+shader\s+(?<shader>[^\r\n]+)", RegexOptions.IgnoreCase|RegexOptions.IgnoreCase|RegexOptions.Compiled);

        static private TikData ParseTik(string path)
        {
            string tikContent = File.ReadAllText(path);
            TikData retVal = new TikData();
            Match match;
            MatchCollection matches;
            if ((match = skelModelRegex.Match(tikContent)).Success){
                retVal.skelModel = match.Groups["skelModel"].Value.Trim();
            }
            if ((match = scaleRegex.Match(tikContent)).Success){
                retVal.scale = float.Parse(match.Groups["scale"].Value.Trim());
            }
            if ((matches = surfaceShaderRegex.Matches(tikContent)).Count > 0){
                foreach(Match matchHere in matches)
                {
                    retVal.surfaceToShaderMappings[matchHere.Groups["surface"].Value.Trim()] = matchHere.Groups["shader"].Value.Trim();
                }
            }

            return retVal;
        }

        static string[] crawlDirectory(string dir)
        {
            if (!Directory.Exists(dir))
            {
                return new string[0];
            }
            List<string> filesAll = new List<string>();
            filesAll.AddRange(Directory.GetFiles(dir));
            string[] dirs = Directory.GetDirectories(dir);
            foreach (string subdir in dirs)
            {
                filesAll.AddRange(crawlDirectory(subdir));
            }

            return filesAll.ToArray();
        }
    }
}
