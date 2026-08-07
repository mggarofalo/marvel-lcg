from . import *

# Jet Boots

def GetAbilities() -> Sequence['Ability']:

    def jet_boots(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        value = initiator.dealt_encounter_cards.GetSize()
        message.PreventDamage(value, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Star-Lord"),
            trait="AERIAL",
        ),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.HeroInterrupt,
            CardFinder(name="Star-Lord"),
            jet_boots,
            conditions=[
                lambda effect, message:
                    effect.GetInitiator().dealt_encounter_cards.GetSize() > 0,
            ]
        ).SetCostFunc(CostFunc.Exhaust("This"))
    ]

