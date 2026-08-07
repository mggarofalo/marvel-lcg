from . import *

# Shields Up

def GetAbilities() -> Sequence['Ability']:

    def shields_up_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        minions = player.GetEngagedMinions(CardFinder2("SENTINEL", Minion))

        gave = False
        if minions:
            gave = Faces.GiveStatus(minions, "Tough", effect)

        if not gave:
            ThisCardGainSurge(effect)

    def shields_up_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = Worlds.FindVillain(effect)
        if villain:
            Faces.GiveStatus([villain], "Tough", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            shields_up_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            shields_up_boost
        ),
    ]

