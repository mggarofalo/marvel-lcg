from . import *

# Sound Manipulation

def GetAbilities() -> Sequence['Ability']:

    def sound_manipulation_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)

        villain = Worlds.FindCardOnField(
            effect,
            name="Klaw",
            card_type=Villain
        )

        if villain:
            if not this.HealthUnits([villain], 4, effect):
                this.GainSurge(1, effect)

    def sound_manipulation_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)

        player = message.GetToPlayer()
        hero = player.GetIdentity()
        hero.TakeDamage(this, 2, effect)

        villain = Worlds.FindCardOnField(
            effect,
            name="Klaw",
            card_type=Villain
        )

        if villain:
            this.HealthUnits([villain], 2, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            sound_manipulation_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            sound_manipulation_hero
        )
    ]
