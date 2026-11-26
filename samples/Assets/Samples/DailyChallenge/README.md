# Daily Challenge

Track progress against a simulated day and award its reward once. This independent sample draws on Cristiano Piromalli’s work on daily challenges and progression. The events, day sequence, milestones and rewards are synthetic.

## Try it

1. Open [DailyChallenge.unity](Scenes/DailyChallenge.unity) and press Play. Select **Submit event**, then **Retry previous event**. The retry reports the duplicate and progress stays at 1.
2. Submit two more events. The second reaches the milestone and the third unlocks the reward. Select **Claim reward** twice. The total stays at 1 after the retry.
3. Select **Next simulated day**, then retry the previous event. The old day stamp is rejected and the new day's progress stays at 0. Submit a fresh event to start the challenge for the new day.

## Read and verify

The [ledger](Runtime/Core/DailyChallengeLedger.cs) accepts one contribution per event ID on the current day. A forward day change clears the current challenge, its event IDs and its claim flag. The reward counter records accepted claims across simulated days for the lifetime of the scene. Reopening the scene starts a new demonstration.

Run `dotnet run --project Tests/DailyChallenge/DailyChallenge.Checks.csproj -c Release` from the Unity project directory (`samples/` in the portfolio repository). The [checks](../../../Tests/DailyChallenge/Program.cs) cover milestone and claim progression, duplicate events, invalid and stale inputs, and independent claims across day changes.
