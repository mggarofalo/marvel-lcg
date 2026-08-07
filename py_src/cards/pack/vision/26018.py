from . import *

# Defiance

def GetAbilities() -> Sequence['Ability']:

    def defiance(effect: 'Effect', message: 'Message.WhenBoostCardWouldTurnedFaceUp') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.SetBeInstead(effect)
        Faces.DiscardAll([message.trigger], effect)


    return [
        AbilityFactory.WhenBoostCardWouldTurnedFaceUp(
            AbilityType.HeroInterrupt,
            defiance,
            while_attack=True,
            boost_for_card=Enemy,
            activate_target="You"
        ).SetPlay().SetLabel('defense'),
    ]

