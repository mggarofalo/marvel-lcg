from . import *

# Freedom Fighters

def GetAbilities() -> Sequence['Ability']:

    def freedom_fighters_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        minions = Worlds.GetOnFieldMinions(effect)
        if not Faces.ResolveAbility(minions, player, AbilityType.Boost, effect):
            this.GainSurge(+1, effect)

    def freedom_fighters_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        minions = player.GetEngagedMinions()
        Faces.ResolveAbility(minions, player, AbilityType.Boost, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            freedom_fighters_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            freedom_fighters_boost
        ),
    ]

