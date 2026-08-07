from . import *

# Bulletproof Belle

def GetAbilities() -> Sequence['Ability']:

    def bulletproof_belle(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.PreventDamage("All", effect)

        initiator = effect.GetInitiator()
        identity = initiator.GetIdentity()
        Faces.GiveStatus([identity], "Tough", effect)


    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.HeroInterrupt,
            CardFinder(card_type=Enemy, with_attach=CardFinder(name="Touched")),
            bulletproof_belle
        ).SetPlay().SetLabel('defense'),
    ]

