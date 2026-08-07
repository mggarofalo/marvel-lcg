from . import *

# * Spider-Woman

def GetAbilities() -> Sequence['Ability']:

    def spider_woman_setup(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(Leader)
        Unused(this)

        team = Worlds.GetEnemyTeam(effect)
        Find.FindAndAttachTo(
            effect,
            this,
            who_perform=team,
            name="Finesse"
        )

    def spider_woman(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Leader)
        Unused(this)

        face = FindFinesse(effect)
        damage = face.GetCounters('skill')
        message.GainATKForThisAttack(damage, effect)
        Faces.RemoveCountersOn([face], "All", 'skill', effect)


    return [
        AbilityFactory.WhenCardSetup(
            "This",
            spider_woman_setup
        ),
        AbilityFactory.WhenUnitMakeAttack(
            AbilityType.ForcedInterrupt,
            "This",
            spider_woman,
        )
    ]

