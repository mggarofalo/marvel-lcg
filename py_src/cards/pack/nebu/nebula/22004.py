from . import *

# Cutthroat Ambition

def GetAbilities() -> Sequence['Ability']:

    def cutthroat_ambition(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 3, effect)

    return [
        AbilityFactory.UnitAttackGainKeyword(
            CardFinder(name="Nebula", card_type=Hero),
            piercing=True,
            overkill=True
        ),
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            cutthroat_ambition
        ).SetLabel('thwart').SetTarget(Scheme2),
    ]

