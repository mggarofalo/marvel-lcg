from . import *

# Encased in Ice

def GetAbilities() -> Sequence['Ability']:

    def encased_in_ice(effect: 'Effect', message: 'Message.AfterCardPlacedCounter') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        damage = this.GetCounters('damage') - 3

        this.DealDamage([this.GetBindFace()], damage, effect)
        Faces.DiscardAll([this], effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Identity
        ),
        *AbilityFactory.UnitCannotAttackTarget(
            "AttachedCharacter",
            can_only_attack="This"
        ),
        AbilityFactory.ThisGainKeyword(
            lambda effect, ui: 1,
            consider_as=("ICE", Minion),
        ),
        AbilityFactory.IfCardHasOrMoreCounters(
            "This",
            3,
            'damage',
            encased_in_ice
        ),
    ]

