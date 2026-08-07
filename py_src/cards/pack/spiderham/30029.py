from . import *

# Warrior of the Great Web

def GetAbilities() -> Sequence['Ability']:

    def warrior_of_the_great_web(effect: 'Effect', message: 'Message.AfterCardLeavePlay') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        for face in effect.targets:
            if Unit2.IsType(face):
                face.GainUntilPhaseEnd(
                    effect,
                    attack=1,
                )

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            CardFinder(title_contain="Spider")
        ),
        *AbilityFactory.GiveKeywordToAttached(
            "Character",
            trait="WEB-WARRIOR",
        ),
        AbilityFactory.AfterCardLeavePlay(
            AbilityType.Response,
            Ally,
            warrior_of_the_great_web,
            conditions=[
                lambda effect, message:
                    message.trigger.HasTrait('WEB-WARRIOR') and \
                    CanAttack.IsType(effect.this.GetBindFace()),
            ]
        ).SetTarget("Attached"),
    ]

