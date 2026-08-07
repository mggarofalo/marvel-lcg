from . import *

# Adaptive Plumage

def GetAbilities() -> Sequence['Ability']:

    def adaptive_plumage(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 3, effect)
        Faces.GiveStatus(effect.targets2, "Confused", effect)

    def adaptive_plumage_archangel(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 4, effect)
        Faces.GiveStatus(effect.targets, "Stunned", effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            adaptive_plumage,
            conditions=[
                lambda effect, message:
                    effect.GetInitiator().GetIdentity().IsName("Angel")
            ]
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2)
        .SetTarget2(Enemy, canbe_confused=True),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            adaptive_plumage_archangel,
            conditions=[
                lambda effect, message:
                    effect.GetInitiator().GetIdentity().IsName("Archangel")
            ]
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

