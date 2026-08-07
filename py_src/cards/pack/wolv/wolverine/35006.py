from . import *

# "I Got Better"

def GetAbilities() -> Sequence['Ability']:

    def i_got_better(effect: 'Effect', message: 'Message.WhenUnitWouldBeDefeated') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        hero = this.GetBindFace().CastTo(Unit2)
        message.SetBeInstead(effect)
        hero.SetHealth(5, effect)
        Faces.ReadyAll(effect.targets, effect)
        Faces.DiscardAll([this], effect)

    return [
        AbilityFactory.WhenUnitWouldBeDefeated(
            AbilityType.Interrupt,
            "You",
            i_got_better,
            conditions=[
                lambda effect, message:
                    message.being_atk_message != None and \
                    Enemy.IsType(message.being_atk_message.attacker),
            ]
        ).SetTarget("YourIdentity")
    ]

