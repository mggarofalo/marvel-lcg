from . import *

# Psychic Inertia

def GetAbilities() -> Sequence['Ability']:

    def psychic_inertia(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> bool:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        face = this.GetBindFace()
        def has_attack():
            for unit, _, _ in effect.world.stat.this_turn_made_attack:
                if unit == face:
                    return True
            return False

        def has_thwart():
            for unit, _, _ in effect.world.stat.this_turn_made_thwart:
                if unit == face:
                    return True
            return False

        return has_attack() and has_thwart()

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity"
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
            conditions=[psychic_inertia]
        ),
    ]

