from . import *

# * Gladiator

def GetAbilities() -> Sequence['Ability']:

    def gladiator_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        face = Worlds.FindCardOnField(
            effect,
            name="Trial by Combat"
        )
        if face:
            player.DealEncounterCard(this, effect)


    return [
        AbilityFactory.UnitCannotTakeDamageWhile(
            AbilityType.NonKeyword,
            "This",
            while_face_is_in_play=CardFinder(name="Trial by Combat")
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            gladiator_boost
        ),
    ]

