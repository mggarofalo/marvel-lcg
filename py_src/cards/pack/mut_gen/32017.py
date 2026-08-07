from . import *

# Mutant Protectors

def GetAbilities() -> Sequence['Ability']:

    def mutant_protectors(effect: 'Effect', message: 'Message.WhenUnitBeingAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        for target in effect.targets:
            ally = target.CastTo(Ally)
            ally.PutIntoPlay(initiator, effect)
            Faces.ExhaustAll([ally], effect)
            message.DeclareDefender(ally, effect)

    return [
        AbilityFactory.WhenUnitBeingAttack(
            AbilityType.HeroInterrupt,
            None,
            Enemy,
            mutant_protectors,
        ).SetPlay(only_if_your_identity_has_trait="X-MEN").SetLabel('defense')
        .SetTarget(Ally, trait="X-MEN", from_where=["YourHandCards"])
    ]

