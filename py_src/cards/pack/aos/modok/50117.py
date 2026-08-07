from . import *

# Psionic Force Field

def GetAbilities() -> Sequence['Ability']:

    def psionic_force_field(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.SetBeInstead(effect)

        Faces.PlaceCountersOn([this], message.will_take_damage, 'damage', effect)

        if this.GetCounters('damage') >= 5:
            Faces.DiscardAll([this], effect)

    def psionic_force_field_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        this.AttachTo2(message.activating_enemy, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            MODOK_FINDER
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Enemy,
            stalwart=1,
        ),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.ForcedInterrupt,
            "AttachedEnemy",
            psionic_force_field
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            psionic_force_field_boost
        ),
    ]

