using Akka.Actor;
using Puhu.GitKraken.Models;
using Puhu.GitKraken.Services;
using Puhu.Plugin;

namespace Puhu.GitKraken.Actors;

public sealed class GitStagingActor : ReceiveActor
{
    private readonly GitCliService _cli;

    public GitStagingActor(GitCliService cli)
    {
        _cli = cli;

        Receive<Tick>(_ => { });
        Receive<RefreshRequest>(_ => { });
        Receive<GetStatus>(_ => HandleGetStatus());
        Receive<StageFiles>(msg => HandleStageFiles(msg));
        Receive<UnstageFiles>(msg => HandleUnstageFiles(msg));
        Receive<StageAll>(_ => HandleStageAll());
        Receive<UnstageAll>(_ => HandleUnstageAll());
        Receive<GetFileDiff>(msg => HandleGetFileDiff(msg));
    }

    private void HandleGetStatus()
    {
        var result = _cli.RunAsync("status --porcelain=v2").GetAwaiter().GetResult();
        if (!result.Success)
        {
            Sender.Tell(new StatusResponse(new WorkingTreeStatus([], [], [])));
            return;
        }
        Sender.Tell(new StatusResponse(GitStatusParser.Parse(result.Stdout)));
    }

    private void HandleStageFiles(StageFiles msg)
    {
        foreach (var path in msg.Paths)
        {
            var result = _cli.RunAsync($"add \"{path}\"").GetAwaiter().GetResult();
            if (!result.Success)
            {
                Sender.Tell(new OperationResult(false, result.Stderr));
                return;
            }
        }
        Sender.Tell(new OperationResult(true, null));
    }

    private void HandleUnstageFiles(UnstageFiles msg)
    {
        foreach (var path in msg.Paths)
        {
            var result = _cli.RunAsync($"restore --staged \"{path}\"").GetAwaiter().GetResult();
            if (!result.Success)
            {
                Sender.Tell(new OperationResult(false, result.Stderr));
                return;
            }
        }
        Sender.Tell(new OperationResult(true, null));
    }

    private void HandleStageAll()
    {
        var result = _cli.RunAsync("add -A").GetAwaiter().GetResult();
        Sender.Tell(new OperationResult(result.Success, result.Success ? null : result.Stderr));
    }

    private void HandleUnstageAll()
    {
        var result = _cli.RunAsync("reset HEAD").GetAwaiter().GetResult();
        Sender.Tell(new OperationResult(result.Success, result.Success ? null : result.Stderr));
    }

    private void HandleGetFileDiff(GetFileDiff msg)
    {
        var diffArgs = msg.Staged
            ? $"diff --cached -- \"{msg.Path}\""
            : $"diff -- \"{msg.Path}\"";

        var result = _cli.RunAsync(diffArgs).GetAwaiter().GetResult();

        if (!result.Success || string.IsNullOrWhiteSpace(result.Stdout))
        {
            Sender.Tell(new FileDiffResponse(new FileDiff(msg.Path, ChangeKind.Modified, null, [])));
            return;
        }

        var files = GitDiffParser.Parse(result.Stdout);
        Sender.Tell(new FileDiffResponse(files.Count > 0 ? files[0] : new FileDiff(msg.Path, ChangeKind.Modified, null, [])));
    }
}
