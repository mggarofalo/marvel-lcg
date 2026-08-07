from . import *

# "Now We're Angry!"

def GetAbilities() -> Sequence['Ability']:

    def now_were_angry(effect: 'Effect', message: 'Message.AfterUnitTookDamage') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        Faces.RemoveCountersOn([this], 1, 'rage', effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Venom")
        ),
        AbilityFactory.UnitAttackGainKeyword(
            CardFinder(name="Venom"),
            overkill=True
        ),
        AbilityFactory.AfterUnitTookDamage(
            AbilityType.ForcedResponse,
            CardFinder(name="Venom"),
            now_were_angry,
            is_from_attack=True,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            RevealThisCard,
        ),
    ]

