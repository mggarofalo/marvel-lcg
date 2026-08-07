from . import *

# * J. Jonah Jameson

def GetAbilities() -> Sequence['Ability']:

    def j_jonah_jameson(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()

        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            name="Get the Scoop"
        )
        if face:
            face.PutIntoPlay(initiator, effect)

    def j_jonah_jameson_action(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 2, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            j_jonah_jameson
        ).SetCostFunc(CostFunc.Exhaust("This")),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            j_jonah_jameson_action
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(SchemeSide2),
    ]

