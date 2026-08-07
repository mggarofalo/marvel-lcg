from . import *

# * Venom: Eddie Brock

def GetAbilities() -> Sequence['Ability']:

    def venom(effect: 'Effect', message: 'Message.AfterCardRevealedEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        face = message.trigger
        if EncounterNonVillainCard.IsType(face):
            value = face.boost_star + face.boost_const
            this.DealDamage(effect.targets, value, effect)

    return [
        AbilityFactory.AfterPlayerRevealCard(
            AbilityType.Response,
            "You",
            EncounterCard,
            venom
        ).SetCostFunc(CostFunc.DealDamage(1, "This"))
        .SetTarget(Enemy),
    ]

