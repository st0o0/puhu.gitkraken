using Akka.Actor;
using Puhu.GitKraken.Models;
using Puhu.GitKraken.Services;

namespace Puhu.GitKraken.Actors;

public sealed class GitWriteActor : ReceiveActor
{
    private readonly GitCliService _cli;

    private const string BranchFormat = "%(refname:short)%09%(objectname:short)%09%(HEAD)%09%(upstream:short)";

    public GitWriteActor(GitCliService cli)
    {
        _cli = cli;

        Receive<CreateCommit>(HandleCreateCommit);
        Receive<CreateBranch>(HandleCreateBranch);
        Receive<SwitchBranch>(HandleSwitchBranch);
        Receive<DeleteBranch>(HandleDeleteBranch);
        Receive<RenameBranch>(HandleRenameBranch);
        Receive<GetBranches>(_ => HandleGetBranches());
        Receive<MergeBranch>(HandleMergeBranch);
        Receive<RebaseBranch>(HandleRebaseBranch);
        Receive<AbortMerge>(_ => HandleAbortMerge());
        Receive<AbortRebase>(_ => HandleAbortRebase());
        Receive<StashChanges>(HandleStashChanges);
        Receive<StashPop>(_ => HandleStashPop());
    }

    private void HandleCreateCommit(CreateCommit msg)
    {
        var escapedMessage = msg.Message.Replace("\"", "\\\"");
        var amendFlag = msg.Amend ? " --amend" : "";
        var result = _cli.RunAsync($"commit -m \"{escapedMessage}\"{amendFlag}").GetAwaiter().GetResult();

        if (!result.Success)
        {
            Sender.Tell(new CommitResult(false, null, result.Stderr.Trim()));
            return;
        }

        var shaResult = _cli.RunAsync("rev-parse --short HEAD").GetAwaiter().GetResult();
        var sha = shaResult.Success ? shaResult.Stdout.Trim() : null;
        Sender.Tell(new CommitResult(true, sha, null));
    }

    private void HandleCreateBranch(CreateBranch msg)
    {
        var startPoint = msg.StartPoint is not null ? $" \"{msg.StartPoint}\"" : "";
        var result = _cli.RunAsync($"branch \"{msg.Name}\"{startPoint}").GetAwaiter().GetResult();
        Sender.Tell(new OperationResult(result.Success, result.Success ? null : result.Stderr.Trim()));
    }

    private void HandleSwitchBranch(SwitchBranch msg)
    {
        var result = _cli.RunAsync($"checkout \"{msg.Name}\"").GetAwaiter().GetResult();
        Sender.Tell(new OperationResult(result.Success, result.Success ? null : result.Stderr.Trim()));
    }

    private void HandleDeleteBranch(DeleteBranch msg)
    {
        var flag = msg.Force ? "-D" : "-d";
        var result = _cli.RunAsync($"branch {flag} \"{msg.Name}\"").GetAwaiter().GetResult();
        Sender.Tell(new OperationResult(result.Success, result.Success ? null : result.Stderr.Trim()));
    }

    private void HandleRenameBranch(RenameBranch msg)
    {
        var result = _cli.RunAsync($"branch -m \"{msg.OldName}\" \"{msg.NewName}\"").GetAwaiter().GetResult();
        Sender.Tell(new OperationResult(result.Success, result.Success ? null : result.Stderr.Trim()));
    }

    private void HandleGetBranches()
    {
        var result = _cli.RunAsync($"for-each-ref --format=\"{BranchFormat}\" \"refs/heads/\" \"refs/remotes/\"").GetAwaiter().GetResult();
        if (!result.Success)
        {
            Sender.Tell(new BranchesResponse([]));
            return;
        }
        Sender.Tell(new BranchesResponse(GitBranchParser.Parse(result.Stdout, '\t')));
    }

    private void HandleMergeBranch(MergeBranch msg)
    {
        var result = _cli.RunAsync($"merge \"{msg.Name}\"").GetAwaiter().GetResult();

        if (result.Success)
        {
            Sender.Tell(new MergeRebaseResult(ResultKind.Success, null, null));
            return;
        }

        if (result.Stdout.Contains("CONFLICT") || result.Stderr.Contains("CONFLICT"))
        {
            var conflictResult = _cli.RunAsync("diff --name-only --diff-filter=U").GetAwaiter().GetResult();
            var conflictCount = conflictResult.Success
                ? conflictResult.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length
                : 0;
            Sender.Tell(new MergeRebaseResult(ResultKind.Conflict, result.Stderr.Trim(), conflictCount));
            return;
        }

        Sender.Tell(new MergeRebaseResult(ResultKind.Error, result.Stderr.Trim(), null));
    }

    private void HandleRebaseBranch(RebaseBranch msg)
    {
        var result = _cli.RunAsync($"rebase \"{msg.Onto}\"").GetAwaiter().GetResult();
        if (result.Success)
        {
            Sender.Tell(new MergeRebaseResult(ResultKind.Success, null, null));
            return;
        }

        if (result.Stderr.Contains("CONFLICT"))
        {
            Sender.Tell(new MergeRebaseResult(ResultKind.Conflict, result.Stderr.Trim(), null));
            return;
        }

        Sender.Tell(new MergeRebaseResult(ResultKind.Error, result.Stderr.Trim(), null));
    }

    private void HandleAbortMerge()
    {
        var result = _cli.RunAsync("merge --abort").GetAwaiter().GetResult();
        Sender.Tell(new OperationResult(result.Success, result.Success ? null : result.Stderr.Trim()));
    }

    private void HandleAbortRebase()
    {
        var result = _cli.RunAsync("rebase --abort").GetAwaiter().GetResult();
        Sender.Tell(new OperationResult(result.Success, result.Success ? null : result.Stderr.Trim()));
    }

    private void HandleStashChanges(StashChanges msg)
    {
        var messageArg = msg.Message is not null ? $" push -m \"{msg.Message.Replace("\"", "\\\"")}\"" : "";
        var result = _cli.RunAsync($"stash{messageArg}").GetAwaiter().GetResult();
        Sender.Tell(new StashResult(result.Success, result.Success ? null : result.Stderr.Trim()));
    }

    private void HandleStashPop()
    {
        var result = _cli.RunAsync("stash pop").GetAwaiter().GetResult();
        Sender.Tell(new StashResult(result.Success, result.Success ? null : result.Stderr.Trim()));
    }
}
