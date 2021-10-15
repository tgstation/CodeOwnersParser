using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using CodeOwnersNotifier.Github.Pulls;

namespace CodeOwnersNotifier
{
    static class Helpers
    {
        //Dictionary too lookup already generated Regex for paths
        static Dictionary<string, string> pathRegexLookup = new Dictionary<string, string>();

        public static Dictionary<string, List<string>> ParseCodeownersFile(string filepath)
        {
            Dictionary<string, List<string>> returnList = new Dictionary<string, List<string>>();
            try
            {
                // Create an instance of StreamReader to read from a file.
                // The using statement also closes the StreamReader.
                using (StreamReader sr = new StreamReader(filepath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.StartsWith('#') || String.IsNullOrEmpty(line))
                            continue;

                        //Split at whitespace, unless escaped
                        string[] parsedLine = Regex.Split(line, @"(?<!\\)\s");
                        string path = parsedLine[0];

                        List<string> currentList = null;
                        foreach (string owner in parsedLine[1..])
                        {
                            if (returnList.TryGetValue(owner, out currentList))
                            {
                                currentList.Add(path);
                            }
                            else
                            {
                                returnList.Add(owner, new List<string>() { path });
                            }
                        }

                    }
                }
                return returnList;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error parsing Codeowner file:");
                Console.WriteLine(e.Message);
                return returnList;
            }
        }

        /// <summary>
        /// Get list of people whos files got modfied
        /// </summary>
        /// <param name="ownersWithFiles">Dictionary containing owners and their paths</param>
        /// <param name="modfiedFiles">Github PR files that were modfied in the PR</param>
        /// <returns></returns>
        public static List<string> GetOwnersWithModifiedFiles(Dictionary<string, List<string>> ownersWithFiles, List<PRFile> modifiedFiles)
        {
            return ownersWithFiles
                .Where(
                    ownerWithFilesKvp => modifiedFiles
                        .Select(modifiedFile => modifiedFile.filename)
                        .Any(
                            filename => ownerWithFilesKvp.Value.Any(
                                ownerFile => FileMatchesPath(filename, ownerFile))))
                .Select(ownerWithFilesKvp => ownerWithFilesKvp.Key)
                .Distinct()
                .ToList();

        }

        /// <summary>
        /// Takes a filepath and codeowner path and checks if file matches that path
        /// </summary>
        /// <param name="file">Relative path of the file e.g. /src/CodeOwnerNotifier/Program.cs</param>
        /// <param name="path">Codeowner path to check against e.g. /src/**</param>
        /// <returns></returns>
        public static bool FileMatchesPath(string file, string path)
        {
            return Regex.IsMatch(file, GenerateRegexForCodeownerPath(path));
        }

        /// <summary>
        /// Generate Regex for codeowner path entry
        /// </summary>
        /// <param name="path">Codeowner entry</param>
        /// <returns></returns>
        public static string GenerateRegexForCodeownerPath(string path)
        {
            //File: ^.*(?<=(\/|^))(Test1\.txt)$
            // ^.*(?<=(\/|^))(Test1\.txt)(?=$|\/).*$
            string regexString = "";
            //Entry ends with / aka only mathches folders not files
            bool folderOnly = false;
            //No slash at the beginning or middle, can match at any depth
            bool anyDepth = false;
            //No slashes at all, match any file at any level
            bool fileMode = false;

            //If Regex for entry was already calcualted return that
            if (pathRegexLookup.TryGetValue(path.TrimStart('/'), out regexString))
            {
                return regexString;
            }

            if (path.EndsWith("/"))
                folderOnly = true;
            if (path[0..(path.Length - 1)].Contains("/"))
                anyDepth = true;
            if (!path.Contains("/"))
                fileMode = true;

            //Remove leading slash before generating Regex as modified files from PR don't start with slash aka src/code/Program.cs and not /src...
            path = path.TrimStart('/');
            Regex.Escape(path);

            path = path.Replace("*", @"[^\/]*");
            path = path.Replace("?", @"[^\/]");

            if (fileMode)
            {
                regexString = @$"(?<=(\/|^))({path})(?=$)";
                pathRegexLookup.Add(path, regexString);
                return regexString;
            }

            else
            {
                regexString = path;
                pathRegexLookup.Add(path, regexString);
                return regexString;
            }
            
        }

        /// <summary>
        /// Get already mentioned owners from a list of Github comments
        /// </summary>
        /// <param name="comments">List of Github comments</param>
        /// <param name="username">Name of the user posting the mentions</param>
        /// <param name="bodyPrefix">Prefix of notify comments</param>
        /// <returns></returns>
        public static List<string> GetMentionedOwners(List<PRComment> comments, string username, string bodyPrefix)
        {
            return comments
                .Where(
                    comment => comment.user.login == username && comment.body.StartsWith(bodyPrefix))
                .Select(comment => comment.body[bodyPrefix.Length..])
                .SelectMany(owners => owners.Split(" ").ToList())
                .Distinct()
                .ToList();

        }
    }
}
