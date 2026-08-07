from . import *

# * Robert Kelly

def GetAbilities() -> Sequence['Ability']:

    def robert_kelly(effect: 'Effect', message: 'Message.WhenFaceWouldDealDamage') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        message.DealThisDamageTo(this, effect)

    return [
        AbilityFactory.FirstPlayerControlThis(),
        *AbilityFactory.ThisNotCountAllyLimit(),
        AbilityFactory.ThisCannotHaveCardsAttached(),
        AbilityFactory.WhenDamageWouldBeDealtTo(
            AbilityType.ForcedInterrupt,
            "You",
            robert_kelly,
            who_deal_damage=Enemy,
            is_undefended_attack=True
        ),
        AbilityFactory.ThisCanAttack(
            conditions=False
        ),
        AbilityFactory.ThisCanThwart(
            conditions=False
        )
    ]

