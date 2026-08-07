from . import *

# Dance of Death

def GetAbilities() -> Sequence['Ability']:

    def dance_of_death_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        def action(faces: Sequence['CardFace']):
            if len(faces) > 0:
                this.DealDamage([faces[0]], 1, effect)
            if len(faces) > 1:
                this.DealDamage([faces[1]], 2, effect)
            if len(faces) > 2:
                this.DealDamage([faces[2]], 3, effect)

        player = message.GetToPlayer()
        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                action
            ).SetTarget("YouControlUnit", range=("Zero", 3))
        )

    def dance_of_death_preparation(effect: 'Effect', message: 'Message.WhenResolvePreparationAbility') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        if player:
            this.DealDamage(player.GetControlCharacters(), 1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            dance_of_death_revealed
        ),
        AbilityFactory.WhenResolvePreparationAbility(
            "This",
            dance_of_death_preparation
        ),
    ]

