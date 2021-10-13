using System;
using System.Linq;
using CommandLine;
using static CommandLine.Parser;
using CodeOwnersNotifier;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Net.Http;
using CodeOwnersNotifier.Github.Pulls;

var parser = Default.ParseArguments<ActionInputs>(() => new(), args);
parser.WithNotParsed(
    errors =>
    {
        Console.Write(errors);
        Environment.Exit(2);
    });

parser.WithParsed(options => NotifyOwners(options));

static void NotifyOwners(ActionInputs inputs)
{
    Console.WriteLine($"Parsing codeowner file at: {inputs.WorkspaceDirectory}{inputs.file}");
    Dictionary<string, List<string>> codeowners = Helpers.ParseCodeownersFile(inputs.WorkspaceDirectory + inputs.file);

    HttpClient httpClient = new HttpClient();
    httpClient.BaseAddress = new Uri("https://api.github.com");
    //Add User-Agent otherwise Github API will return 403
    httpClient.DefaultRequestHeaders.Add("User-Agent", "CodeownersNotifier");
    Console.WriteLine($"Getting PR files from: {httpClient.BaseAddress}repos/{inputs.Owner}/{inputs.Name}/pulls/{inputs.pullID}/files");
    PRFile[] modifiedFiles = httpClient.GetFromJsonAsync<PRFile[]>($"repos/{inputs.Owner}/{inputs.Name}/pulls/{inputs.pullID}/files").Result;

    List<string> owners = Helpers.GetOwnersWithModifiedFiles(codeowners, modifiedFiles.ToList());
}

