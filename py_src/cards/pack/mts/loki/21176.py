from . import *

# The Trickster

def GetAbilities() -> Sequence['Ability']:

    def the_trickster_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        SwapLokiWithRandomSetAsideLokiVillain(effect)
        villain = Worlds.FindVillain(effect, name="Loki")
        if villain:
            villain.DoActivate(player, effect)

    def the_trickster_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = Worlds.FindVillain(effect, name="Loki")
        if villain:
            message.GiveBoostCardForThisActivation(villain, 1, effect)
            Faces.GiveStatus([villain], "Tough", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_trickster_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            the_trickster_boost
        ),
    ]

