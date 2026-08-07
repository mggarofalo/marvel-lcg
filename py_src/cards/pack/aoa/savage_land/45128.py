from . import *

# Pterosaur

def GetAbilities() -> Sequence['Ability']:

    def pterosaur_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        ResolveSpecialAbilityOnTheSETTINGEnvironment(player, effect)


    def pterosaur_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        face = Worlds.FindCardOnField(
            effect,
            name="The Savage Land",
        )
        if face:
            player.DealEncounterCard(this, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            pterosaur_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            pterosaur_boost
        ),
    ]

