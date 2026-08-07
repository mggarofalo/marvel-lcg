from . import *

# Spider Reflexes

def GetAbilities() -> Sequence['Ability']:

    def spider_reflexes(effect: 'Effect', message: 'Message.WhenUnitWouldDefend') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()

        value = GetTuckCardSameSetSize(initiator, [message.attacker], effect)
        message.GainDEFForThisAttack(value, effect)

        def action():
            face = Worlds.GetEncounterDiscardPile(effect).GetTop()
            if face:
                TuckCardUnderSilk(face, effect)
        RunAt.AfterEventEnd(effect, message, action)

    return [
        AbilityFactory.WhenUnitDefendAgainstAttack(
            AbilityType.HeroInterrupt,
            CardFinder(name="Silk"),
            spider_reflexes,
            against_who=Enemy,
            is_basic_defense=True
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

