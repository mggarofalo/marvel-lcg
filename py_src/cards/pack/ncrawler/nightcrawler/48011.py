from . import *

# Tally Ho!

def GetAbilities() -> Sequence['Ability']:

    def tally_ho(effect: 'Effect', message: 'Message.AfterUnitBecomeDefender') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        face = message.by_effect.this

        Faces.ReturnToHand([face], initiator, effect)
        if message.attacker:
            this.DealDamage([message.attacker], 3, effect)


    return [
        AbilityFactory.AfterUnitBecomeDefender(
            AbilityType.HeroResponse,
            tally_ho,
            by_effect_face=CardFinder(name="Bamf!"),
        ).SetPlay().SetLabel('defense'),
    ]

