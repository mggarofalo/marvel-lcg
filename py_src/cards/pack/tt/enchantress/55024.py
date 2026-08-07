from . import *

# Magical Restraints

def GetAbilities() -> Sequence['Ability']:

    def magical_restraints_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if not identity.HasTrait("DEFIANT") and not identity.HasTrait("ENTHRALLED"):
            return

        def action(targets: Sequence['CardFace'], status: "CardFace.STATUS"):
            Faces.GiveStatus(targets, status, effect)
            PlaceCharmCounter(player, 1, effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "You are stunned.",
                lambda targets:
                    action(targets, "Stunned")
            ).SetTarget("YourIdentity", canbe_stunned=True),
            AbilityFactory.ForChoiceAbility(
                "You are confused.",
                lambda targets:
                    action(targets, "Confused")
            ).SetTarget("YourIdentity", canbe_confused=True),
            choose_x_different_options=2 if identity.HasTrait("ENTHRALLED") else 1
        )



    return [
        AbilityFactory.WhenThisRevealed(
            None,
            magical_restraints_revealed
        ),
    ]

