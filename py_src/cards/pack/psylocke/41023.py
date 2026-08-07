from . import *

# * X-Bunker

def GetAbilities() -> Sequence['Ability']:

    def x_bunker(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        for identity in effect.targets:
            value = Worlds.VictoryDisplay(effect).FindCardSize(CardFinder(card_type=SchemeSide2))
            player = identity.GetControlByPlayer()
            face = Search.PlayerDeckTop(
                effect,
                player,
                value,
            )
            if face:
                player.GainCard(face, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            x_bunker
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Identity, trait="MUTANT"),
    ]

