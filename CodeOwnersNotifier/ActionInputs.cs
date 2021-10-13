using System;
using CommandLine;

namespace CodeOwnersNotifier
{
    public class ActionInputs
    {
        string _repositoryName = null!;
        string _branchName = null!;

        public ActionInputs()
        {

        }

        [Option('o', "owner",
            Required = true,
            HelpText = "The owner, for example: \"dotnet\". Assign from `github.repository_owner`.")]
        public string Owner { get; set; } = null!;

        [Option('n', "name",
            Required = true,
            HelpText = "The repository name, for example: \"samples\". Assign from `github.repository`.")]
        public string Name
        {
            get => _repositoryName;
            set => ParseAndAssign(value, str => _repositoryName = str);
        }
        [Option('w', "workspace",
            Required = false,
            HelpText = "The workspace directory, or repository root directory. Use `/github/workspace`.",
            Default = "/github/workspace")]
        public string WorkspaceDirectory { get; set; } = null!;

        [Option('f', "file",
            Required = false,
            HelpText = "Path to the codeowners file.",
            Default = "/.github/CODEOWNERS")]
        public string file { get; set; } = null!;

        [Option('p', "pullID",
           Required = true,
           HelpText = "ID of the PR. Assign from `github.event_path.pull_request.number`.")]
        public string pullID { get; set; } = null!;

        static void ParseAndAssign(string? value, Action<string> assign)
        {
            if (value is { Length: > 0 } && assign is not null)
            {
                assign(value.Split("/")[^1]);
            }
        }
    }
}
