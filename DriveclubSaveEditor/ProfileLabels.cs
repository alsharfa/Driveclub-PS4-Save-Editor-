namespace DriveclubSaveEditor;

internal static class ProfileLabels
{
    internal sealed record CounterLabel(string Category, string Name, string Note = "");

    internal static readonly IReadOnlyDictionary<string, CounterLabel> Counters =
        new Dictionary<string, CounterLabel>(StringComparer.Ordinal)
        {
            ["best_in_accolade_group_cars"] = new("Accolades", "Best Vehicle Accolade Progress"),
            ["best_in_accolade_group_event"] = new("Accolades", "Best Event Accolade Progress"),
            ["best_in_accolade_group_gameplay"] = new("Accolades", "Best Gameplay Accolade Progress"),
            ["best_in_accolade_group_race"] = new("Accolades", "Best Race / Game Mode Accolade Progress"),
            ["challenges_won_for_trophies"] = new("Challenges", "Challenges Won — Trophy Counter"),
            ["my_solo_challenges_won_for_trophies"] = new("Challenges", "My Solo Challenges Won — Trophy Counter"),
            ["solo_challenges_won_for_trophies"] = new("Challenges", "Solo Challenges Won — Trophy Counter"),
            ["faceoff_value"] = new("Trophy Progress", "Face-Off Trophy Progress — Raw"),
            ["fullhouse_won_in_hothatch"] = new("Full House", "Hot Hatch Race Wins"),
            ["fullhouse_won_in_performance"] = new("Full House", "Performance Race Wins"),
            ["fullhouse_won_in_sports"] = new("Full House", "Sports Race Wins"),
            ["fullhouse_won_in_super"] = new("Full House", "Super Race Wins"),
            ["fullhouse_won_in_hyper"] = new("Full House", "Hyper Race Wins"),
            ["fullhouse_won_in_superbike"] = new("Bikes", "Superbike Race Wins"),
            ["lifer_value"] = new("Trophy Progress", "Lifer Distance Counter — Raw"),
            ["longdistance_value"] = new("Trophy Progress", "Long Distance Counter — Raw"),
        };

    internal static CounterLabel GetCounter(string key) =>
        Counters.TryGetValue(key, out var value)
            ? value
            : new CounterLabel("Other", FriendlyNames.Humanize(key));
}
