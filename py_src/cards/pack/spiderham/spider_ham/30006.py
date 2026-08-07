from . import *

# Petulant Pig

def GetAbilities() -> Sequence['Ability']:

    def petulant_pig(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        StickYourTongueOutAtTheVillain()

        initiator = effect.GetInitiator()

        for villain in effect.targets:
            villain.CastTo(Villain).DoAttackYou(initiator, effect)

        initiator.DrawUp(3, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            petulant_pig
        ).SetPlay().SetLabel()
        .SetTarget(Villain),
    ]

