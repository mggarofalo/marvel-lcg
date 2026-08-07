from . import *

# Stepping Disc

def GetAbilities() -> Sequence['Ability']:

    def stepping_disc(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.ReadyAll(effect.targets, effect)

        Faces.MoveAllToDeck(effect.targets2, initiator.player_deck, "Top", effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            stepping_disc,
        ).SetPlay().SetLabel()
        .SetTarget("YourHero", canbe_ready=True)
        .SetTarget2(card_class="IdentitySpecific", non_name="Stepping Disc", from_where=["YourDiscardPile"]),
    ]

