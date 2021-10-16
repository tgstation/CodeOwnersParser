using System;
using System.Linq;
using CommandLine;
using static CommandLine.Parser;
using CodeOwnersParser;
using System.Collections.Generic;
using Octokit;

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

    GitHubClient ghclient = new GitHubClient(new ProductHeaderValue("CodeOwnersParser"));
    ghclient.SetRequestTimeout(TimeSpan.FromMilliseconds(inputs.timeout));
    if (!String.IsNullOrEmpty(inputs.token))
        ghclient.Credentials = new Credentials(inputs.token);

    Console.WriteLine($"Getting PR files for PR with ID {inputs.pullID}");
    List<Octokit.PullRequestFile> modifiedFiles = new List<Octokit.PullRequestFile>(ghclient.PullRequest.Files(inputs.Owner, inputs.Name, inputs.pullID).Result);

    List<string> ownersWithModifiedFiles = Helpers.GetOwnersWithModifiedFiles(codeowners, modifiedFiles);
    //If we were provided a botname parse its comments and find already notifed owners
    if (inputs.botname is not null)
    {
        List<Octokit.IssueComment> PRcomments = new List<IssueComment>(ghclient.Issue.Comment.GetAllForIssue(inputs.Owner, inputs.Name, inputs.pullID).Result);
        List<string> notifiedOwners = Helpers.GetMentionedOwners(PRcomments, inputs.botname, inputs.prefix, inputs.sufix, inputs.separator);
        ownersWithModifiedFiles = ownersWithModifiedFiles.Except(notifiedOwners).ToList();
    }

    string output = String.Join(inputs.separator, ownersWithModifiedFiles);

    Console.WriteLine($"Owners with file changes: {output}");
    Console.WriteLine($"::set-output name=owners::{output}");
    Console.WriteLine($"Output: {inputs.prefix + output + inputs.sufix}");
    Console.WriteLine($"::set-output name=owners-formatted::{inputs.prefix + output + inputs.sufix}");
}

