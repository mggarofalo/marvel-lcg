from . import *

# Heart of the Panther

def GetAbilities() -> Sequence['Ability']:

    def heart_of_the_panther(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            card_type=Upgrade,
            trait="BLACK PANTHER"
        )
        if face:
            face.PutIntoPlay(initiator, effect)

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    initiator.ResolveSpecialAbility(targets, effect)
            ).SetTarget(Upgrade, trait="BLACK PANTHER", range=(1, 4))
        )

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            heart_of_the_panther
        ).SetPlay().SetLabel()
        .SetTarget("TeamUp"),
    ]

