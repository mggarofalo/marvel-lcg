from . import *

# Bodyslide

def GetAbilities() -> Sequence['Ability']:

    def bodyslide(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.GetIdentity().ChangeToOtherIdentityForm(effect)
        type_identity = type(initiator.GetIdentity())

        def action(player: 'Player'):
            if player != initiator:
                player.MayChooseOneAbility(
                    effect,
                    AbilityFactory.ForChoiceAbility(
                        "Change to the form you are in",
                        lambda targets:
                            player.GetIdentity().ChangeToForm(type_identity, effect),
                        condition=not type_identity.IsType(player.GetIdentity())
                    )
                )

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            bodyslide
        ).SetPlay().SetLabel(),
    ]

