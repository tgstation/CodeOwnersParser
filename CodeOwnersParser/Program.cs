using System;
using System.Linq;
using CommandLine;
using static CommandLine.Parser;
using CodeOwnersParser;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Net.Http;
using CodeOwnersParser.Github.Pulls;

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
    httpClient.DefaultRequestHeaders.Add("User-Agent", "CodeOwnersParser");
    Console.WriteLine($"Getting PR files from: {httpClient.BaseAddress}repos/{inputs.Owner}/{inputs.Name}/pulls/{inputs.pullID}/files");
    PRFile[] modifiedFiles = httpClient.GetFromJsonAsync<PRFile[]>($"repos/{inputs.Owner}/{inputs.Name}/pulls/{inputs.pullID}/files").Result;

    List<string> ownersWithModifiedFiles = Helpers.GetOwnersWithModifiedFiles(codeowners, modifiedFiles.ToList());
    //If we were provided a botname parse its comments and find already notifed owners
    if (inputs.botname is not null)
    {
        PRComment[] PRcomments = httpClient.GetFromJsonAsync<PRComment[]>($"repos/{inputs.Owner}/{inputs.Name}/issues/{inputs.pullID}/comments").Result;
        List<string> notifiedOwners = Helpers.GetMentionedOwners(PRcomments.ToList(), inputs.botname, inputs.prefix, inputs.sufix, inputs.seperator);
        ownersWithModifiedFiles = ownersWithModifiedFiles.Except(notifiedOwners).ToList();
    }

    string output = String.Join(inputs.separator, ownersWithModifiedFiles);

    Console.WriteLine($"Owners with file changes: {output}");
    Console.WriteLine($"::set-output name=owners::{output}");
    Console.WriteLine($"Output: {inputs.prefix + output + inputs.sufix}");
    Console.WriteLine($"::set-output name=owners-formatted::{inputs.prefix + output + inputs.sufix}");
}

