from . import *

# Plan of Attack

def GetAbilities() -> Sequence['Ability']:

    def plan_of_attack(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        size = 4
        if initiator.IsAlterEgo():
            size = 7

        face = Search.PlayerDeckTop(
            effect,
            initiator,
            size,
            card_type=Event,
            trait="ATTACK",
        )
        if face:
            initiator.GainCard(face, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            plan_of_attack
        ).SetPlay().SetLabel(),
    ]

