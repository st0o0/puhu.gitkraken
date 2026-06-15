using Akka.Actor;
using Puhu.GitKraken.Services;

namespace Puhu.GitKraken.Actors;

public sealed class GitRemoteActor : ReceiveActor
{
    private readonly GitCliService _cli;
    private static readonly TimeSpan RemoteTimeout = TimeSpan.FromSeconds(120);

    public GitRemoteActor(GitCliService cli)
    {
        _cli = cli;

        Receive<GitPush>(HandlePush);
        Receive<GitPull>(HandlePull);
        Receive<GitFetch>(HandleFetch);
        Receive<GetRemotes>(_ => HandleGetRemotes());
    }

    private void HandlePush(GitPush msg)
    {
        var remote = msg.Remote ?? "origin";
        var branch = msg.Branch is not null ? $" \"{msg.Branch}\"" : "";
        var result = _cli.RunAsync($"push \"{remote}\"{branch}", RemoteTimeout).GetAwaiter().GetResult();
        Sender.Tell(new RemoteOperationResult(result.Success, result.Stdout, result.Success ? null : result.Stderr.Trim()));
    }

    private void HandlePull(GitPull msg)
    {
        var remote = msg.Remote ?? "origin";
        var branch = msg.Branch is not null ? $" \"{msg.Branch}\"" : "";
        var result = _cli.RunAsync($"pull \"{remote}\"{branch}", RemoteTimeout).GetAwaiter().GetResult();
        Sender.Tell(new RemoteOperationResult(result.Success, result.Stdout, result.Success ? null : result.Stderr.Trim()));
    }

    private void HandleFetch(GitFetch msg)
    {
        var args = msg.Remote is not null ? $"fetch \"{msg.Remote}\"" : "fetch --all";
        var result = _cli.RunAsync(args, RemoteTimeout).GetAwaiter().GetResult();
        Sender.Tell(new RemoteOperationResult(result.Success, result.Stdout, result.Success ? null : result.Stderr.Trim()));
    }

    private void HandleGetRemotes()
    {
        var result = _cli.RunAsync("remote -v").GetAwaiter().GetResult();
        if (!result.Success)
        {
            Sender.Tell(new RemotesResponse([]));
            return;
        }
        Sender.Tell(new RemotesResponse(GitBranchParser.ParseRemotes(result.Stdout)));
    }
}
