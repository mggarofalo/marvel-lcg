from . import *

# Direct Assault

def GetAbilities() -> Sequence['Ability']:

    def direct_assault(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        DealDamageToAvengersTower(3, effect)

    def direct_assault_discard(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        Faces.DiscardAll([this], effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(card_type=Villain, is_active_villain=False)
        ),
        AbilityFactory.UnitAttackGainKeyword(
            "AttachedVillain",
            ranged=True
        ),
        AbilityFactory.AfterUnitAttackAndDefeatUnit(
            AbilityType.ForcedInterrupt,
            "AttachedVillain",
            Ally,
            direct_assault,
        ),
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.ForcedInterrupt,
            "AttachedVillain",
            direct_assault_discard,
        ),
    ]

