from . import *

# Radioactive Buildup

def GetAbilities() -> Sequence['Ability']:

    def radioactive_buildup(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        Faces.DiscardAll([this], effect)

    def radioactive_buildup_excess(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Attachment)
        villain = this.GetBindFace().CastTo(Villain)
        scheme = villain.FindCorrespondingScheme()
        if scheme:
            this.PlaceThreatOnSchemes([scheme], message.excess_damage, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name=THUNDERBALL)
        ),
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.NonKeyword,
            CardFinder(name=THUNDERBALL),
            None,
            radioactive_buildup_excess,
            conditions=[
                lambda effect, message:
                    message.excess_damage > 0,
            ]
        ),
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.ForcedResponse,
            CardFinder(name=THUNDERBALL),
            radioactive_buildup
        ),
    ]

