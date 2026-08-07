from . import *

# * Spider-Woman

def GetAbilities() -> Sequence['Ability']:

    def spider_woman_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Leader)
        Unused(this)

        Utility.DealEachPlayerEncounterCard(effect)
        Utility.CannotTakeDamageThisPhase(this, effect)

    def spider_woman(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Leader)
        Unused(this)

        face = Worlds.FindCardOnField(effect, name="Finesse", card_type=Attachment)
        if face:
            damage = face.GetCounters('skill')
            message.GainATKForThisAttack(damage, effect)
            Faces.RemoveCountersOn([face], "All", 'skill', effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            spider_woman_revealed
        ),
        AbilityFactory.WhenUnitMakeAttack(
            AbilityType.ForcedInterrupt,
            "This",
            spider_woman,
        )
    ]

