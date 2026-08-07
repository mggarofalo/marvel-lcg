from . import *

# Out of Reach

def GetAbilities() -> Sequence['Ability']:

    def out_of_reach(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> bool:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        def check():
            if message.would_atk_message and \
                message.would_atk_message.property.ranged:
                return True
            
            if message.would_atk_message and \
                message.would_atk_message.attacker and \
                message.would_atk_message.attacker.HasTrait("AERIAL"):
                return True
            
            if message.would_atk_message and \
                message.would_atk_message.by_effect and \
                message.would_atk_message.by_effect.this.HasTrait("AERIAL"):
                return True

            return False
        
        return not check()

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
        ),
        AbilityFactory.UnitCannotTakeDamageWhile(
            AbilityType.NonKeyword,
            Villain,
            conditions=[out_of_reach]
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("YY"))
        .SetCostFunc(CostFunc.Exhaust("YourHero")),
    ]

