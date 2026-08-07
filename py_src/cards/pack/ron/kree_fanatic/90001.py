from . import *

# * Ronan the Accuser

def GetAbilities() -> Sequence['Ability']:

    def ronan_the_accuser(effect: 'Effect', message: 'Message.WhenPhaseBegin') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        faces = Worlds.FindCardsOnField(effect, card_type=Hero)
        face = Filter.One(faces, effect, fewest_remaining_hp=True)
        if face:
            player = face.GetControlByPlayer()

            this.EngagePlayer(player, effect)

    def ronan_the_accuser_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        this.PutIntoPlay(player, effect)


    return [
        AbilityFactory.UnitCannotBeStunned(
            "This"
        ),
        AbilityFactory.WhenVillainPhaseBegin(
            AbilityType.ForcedInterrupt,
            ronan_the_accuser
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            ronan_the_accuser_boost
        ),
    ]

