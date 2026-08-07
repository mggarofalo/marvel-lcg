from . import *

# Aerial Agility

def GetAbilities() -> Sequence['Ability']:

    def aerial_agility(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.IgnoreBoost(effect, icon=True, ability=True)

    def aerial_agility_archangel(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Tough", effect)
        for face in effect.targets:
            face.TemporaryGain(
                effect,
                message,
                retaliate=+1
            )

    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.HeroInterrupt,
            Enemy,
            aerial_agility,
            conditions=[
                lambda effect, message:
                    effect.GetInitiator().GetIdentity().IsName("Angel")
            ]
        ).SetPlay().SetLabel('defense'),
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.HeroInterrupt,
            Enemy,
            aerial_agility_archangel,
            conditions=[
                lambda effect, message:
                    effect.GetInitiator().GetIdentity().IsName("Archangel")
            ]
        ).SetPlay().SetLabel('defense')
        .SetTarget("YourHero"),
    ]

