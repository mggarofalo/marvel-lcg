from . import *

# Two Against the World

def GetAbilities() -> Sequence['Ability']:

    def two_against_the_world(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            card_type=Upgrade,
            trait="TECH",
        )
        if face:
            face.PutIntoPlay(initiator, effect)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            two_against_the_world
        ).SetPlay().SetLabel()
        .SetTarget("TeamUp"),
    ]

