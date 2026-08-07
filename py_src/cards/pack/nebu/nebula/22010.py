from . import *

# Lethal Intent

def GetAbilities() -> Sequence['Ability']:

    def lethal_intent(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        cost = effect.GetCostX()
        targets = effect.targets[:cost]
        for target in targets:
            target.ResolveSpecialAbility(initiator, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            lethal_intent
        ).SetPlay().SetLabel()
        .SetTarget(Upgrade, trait="TECHNIQUE", from_where=["YouControlCards"], range=(1, "All")),
    ]

