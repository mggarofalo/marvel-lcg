from . import *

# Handspring

def GetAbilities() -> Sequence['Ability']:

    def handspring(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        attacker = message.attacker
        if attacker:
            message.SetBeInstead(effect)

            this.DealDamage([attacker], message.will_take_damage, effect)

            Faces.DiscardAll([this], effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Black Widow"),
            if_cannot_attach_to=Villain
        ),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.ForcedInterrupt,
            "AttachedEnemy",
            handspring,
            is_from_attack=True,
        ),
    ]

