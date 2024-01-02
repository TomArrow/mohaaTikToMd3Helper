using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.RegularExpressions;

namespace mohaaMapDetailTransfer
{
    class Program
    {
        static int detailFlag = 0x8000000;
        static Regex faceParseRegex = new Regex(@"(?<coordinates>(?<coordvec>\((?<vectorPart>\s*[-\d\.]+){3}\s*\)\s*){3})(?<rest>(?:\(\s*(\((?:\s*[-\d\.]+){3}\s*\)\s*){2}\))?\s*(?<texname>[^\s\n]+)\s*(?:\s*[-\d\.]+){3}[^\n]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        class MatchWithParsedFace
        {
            public Vector3[] face;
            public Match match;
            public string rest;
        }

        static void Main(string[] args)
        {
            string sourceMap = args[0];
            string otherEngineMap = args[1];

            int restIndex = args.Length > 2 ? int.Parse(args[2]): 2;

            string sourceText = File.ReadAllText(sourceMap);
            string otherEngineMapText = File.ReadAllText(otherEngineMap);

            MatchCollection sourceMatches = faceParseRegex.Matches(sourceText);

            List<MatchWithParsedFace> realMatches = new List<MatchWithParsedFace>();

            foreach (Match sourceMatch in sourceMatches)
            {
                Vector3[] src = coordTextToTriangle(sourceMatch.Groups["coordinates"].Value);
                realMatches.Add(new MatchWithParsedFace() { face = src, match=sourceMatch, rest = sourceMatch.Groups["rest"].Value });
            }

            int noMatch = 0;
            int alreadyHadFlag = 0;
            int matched = 0;
            int fixedOnes = 0;
            int dupes = 0;


            string fixedText = faceParseRegex.Replace(otherEngineMapText, (match) => {

                string coordText = match.Groups["coordinates"].Value;
                Vector3[] newOne = coordTextToTriangle(coordText);
                string newRest = match.Groups["rest"].Value;

                Match matchingMatch = null;
                string oldRest = null;
                int matches = 0;
                foreach(MatchWithParsedFace sourceMatch in realMatches)
                {
                    Vector3[] src = sourceMatch.face;
                    if (src[0] == newOne[0] && src[1] == newOne[1] && src[2] == newOne[2]
                    || Vector3.Distance(src[0], newOne[0]) < 0.001f &&  Vector3.Distance(src[1], newOne[1]) < 0.001f && Vector3.Distance(src[2], newOne[2]) < 0.001f)
                    {
                        matchingMatch = sourceMatch.match;
                        oldRest = sourceMatch.rest;
                        matches++;
                    }
                }

                if(matches > 1)
                {
                    dupes++;
                }

                if(matches == 0)
                {
                    noMatch++;
                    return match.Value;
                } else if (!oldRest.Contains("+surfaceparm detail"))
                {
                    matched++;
                    return match.Value;
                } else
                {
                    matched++;
                    fixedOnes++;
                    string[] restParts = newRest.Split(' ',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
                    int flags = int.Parse(restParts[restIndex]);
                    if((flags & alreadyHadFlag) > 0)
                    {
                        alreadyHadFlag++;
                    }
                    flags |= detailFlag;
                    restParts[restIndex] = flags.ToString();
                    return coordText + string.Join(' ',restParts);
                }

            });

            File.WriteAllText(Path.ChangeExtension(otherEngineMap, ".fixed.map"), fixedText);

            /*
             
            int noMatch = 0;
            int alreadyHadFlag = 0;
            int matched = 0;
            int fixedOnes = 0;
            int dupes = 0;
             */
            Console.WriteLine($"{noMatch} not matched, processed {fixedOnes}/{matched}, {alreadyHadFlag} already had flag, {dupes} faces had multiple matches.");
        }

        static Regex coordVecRegex = new Regex(@"(?<=\()(?<coordvec>(?<vectorPart>\s*[-\d\.]+){3})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Vector3[] coordTextToTriangle(string coordText)
        {
            List<Vector3> vecs = new List<Vector3>();
            MatchCollection matches = coordVecRegex.Matches(coordText);
            foreach(Match match in matches)
            {
                string[] pointPositionParts = match.Groups["coordvec"].Value.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                vecs.Add(new Vector3(float.Parse(pointPositionParts[0]), float.Parse(pointPositionParts[1]), float.Parse(pointPositionParts[2])));
            }
            return vecs.ToArray();
        }
    }
}
