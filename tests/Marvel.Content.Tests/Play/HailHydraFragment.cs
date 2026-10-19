namespace Marvel.Content.Tests.Play;

// Printed expansion interpretation on synthetic boards until canonical admission.
internal static class HailHydraFragment
{
    internal const string Json = """
    {"cards":[{"card":"04057","abilities":[{"trigger":{"timing":"WhenRevealed","event":"WhenCardRevealed","subject":"this"},"effect":{"seq":[{"enemyAttacks":{"enemies":{"withTrait":{"cards":{"query":"minions"},"trait":"HYDRA"}},"against":"engagedHero"}},{"eachPlayer":{"effect":{"if":{"test":{"not":{"atLeast":{"value":{"activationAttacksAgainstYou":"game"},"count":1}}},"then":{"seq":[{"chooseCard":{"from":{"cardsIn":{"areas":["encounterDeck","encounterDiscardPile"],"kind":"Minion","trait":"HYDRA"}},"effect":{"putIntoPlay":{"card":"chosen","where":"engagedWithYou"}}}},{"shuffle":"encounterDeck"}]}}}}}]}}]},{"card":"04147","abilities":[{"trigger":{"timing":"WhenRevealed","event":"WhenCardRevealed","subject":"this"},"effect":{"seq":[{"enemyAttacks":{"enemies":{"withTrait":{"cards":{"query":"minions"},"trait":"HYDRA"}},"against":"engagedHero"}},{"eachPlayer":{"effect":{"if":{"test":{"not":{"atLeast":{"value":{"activationAttacksAgainstYou":"game"},"count":1}}},"then":{"seq":[{"chooseCard":{"from":{"cardsIn":{"areas":["encounterDeck","encounterDiscardPile"],"kind":"Minion","trait":"HYDRA"}},"effect":{"putIntoPlay":{"card":"chosen","where":"engagedWithYou"}}}},{"shuffle":"encounterDeck"}]}}}}}]}}]}]}
    """;
}
