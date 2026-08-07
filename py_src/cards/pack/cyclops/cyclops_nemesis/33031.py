from . import *

# Concussive Force

def GetAbilities() -> Sequence['Ability']:

    def concussive_force_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        face = Worlds.FindCardOnField(
            effect,
            name="Mister Sinister",
            card_type=Minion
        )

        if face:
            face.DoSchemes(player, effect)
        else:
            this.PlaceThreatOnSchemes("MainScheme", 2, effect)

    def concussive_force_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        face = Worlds.FindCardOnField(
            effect,
            name="Mister Sinister",
            card_type=Minion
        )

        if face:
            face.DoAttackYou(player, effect)
        else:
            identity.TakeIndirectDamage(this, 2, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            concussive_force_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            concussive_force_hero
        ),
    ]

