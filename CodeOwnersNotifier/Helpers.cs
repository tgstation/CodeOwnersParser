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
            return true;
        }

        public static List<string> getMentionedOwners(List<PRComment> comments, string username, string bodyPrefix)
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
