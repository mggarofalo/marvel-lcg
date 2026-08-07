from . import *

# Evasive Maneuvering

def GetAbilities() -> Sequence['Ability']:

    def evasive_maneuvering(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Stun an enemy",
                lambda targets:
                    Faces.GiveStatus(targets, "Stunned", effect)
            ).SetTarget(Enemy, canbe_stunned=True),
            AbilityFactory.ForChoiceAbility(
                "Confuse an enemy",
                lambda targets:
                    Faces.GiveStatus(targets, "Confused", effect)
            ).SetTarget(Enemy, canbe_confused=True),
        )


    return [
        AbilityFactory.UnitIgnoreKeywordIcons(
            CardFinder(name="Nebula", card_type=Hero),
            guard=True,
            patrol=True,
            crisis=True,
        ),
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            evasive_maneuvering
        ).SetTarget2(Enemy, canbe_status=["Confused", "Confused"])
    ]

