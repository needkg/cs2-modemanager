using System;

namespace ModeManager;

internal enum VoteRegistrationStatus
{
    Started,
    Registered,
    AlreadyVoted
}

internal readonly record struct VoteRegistrationOutcome(
    VoteSession Vote,
    VoteRegistrationStatus Status,
    int MissingVotes,
    int RemainingSeconds);

internal sealed class VoteSessionRegistrar
{
    public VoteRegistrationOutcome RegisterVote(
        VoteSession? currentVote,
        ModeDefinition mode,
        string voterId,
        string targetMap,
        bool hasExplicitMapSelection,
        int requiredVotes,
        int voteDurationSeconds)
    {
        var vote = currentVote;
        if (vote == null)
        {
            vote = new VoteSession(
                modeKey: mode.Key,
                modeDisplayName: mode.DisplayName,
                targetMap: targetMap,
                requiredVotes: requiredVotes,
                expiresUtc: DateTime.UtcNow.AddSeconds(voteDurationSeconds));

            vote.VoterIds.Add(voterId);

            return BuildOutcome(vote, VoteRegistrationStatus.Started);
        }

        if (hasExplicitMapSelection)
            vote.TargetMap = targetMap;

        if (vote.VoterIds.Contains(voterId))
            return BuildOutcome(vote, VoteRegistrationStatus.AlreadyVoted);

        vote.VoterIds.Add(voterId);
        return BuildOutcome(vote, VoteRegistrationStatus.Registered);
    }

    private static VoteRegistrationOutcome BuildOutcome(VoteSession vote, VoteRegistrationStatus status) =>
        new(
            Vote: vote,
            Status: status,
            MissingVotes: VoteSessionStore.MissingVotes(vote),
            RemainingSeconds: VoteSessionStore.RemainingSeconds(vote));
}
