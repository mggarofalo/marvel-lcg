from . import *

# * Archangel

def GetAbilities() -> Sequence['Ability']:

    def archangel(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        face = message.played_face
        damage = FacesCounter.GetPrintedCost([face])
        this.DealDamage(effect.targets, damage, effect)


    return [
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.Response,
            "You",
            CardFinder2("AERIAL", Event),
            archangel
        ).SetName("Angel of Death")
        .SetTarget(Enemy)
        .LimitOncePerPhase(),
    ]

