from . import *

# * New Recruits

def GetAbilities() -> Sequence['Ability']:

    def new_recruits(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(PlayerSideScheme)
        Unused(this)

        def action(player: 'Player'):
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        Faces.AddToHand(targets, player, effect)
                ).SetTarget(Ally, trait="NEW", from_where=["SetAside"])
            )

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.CanPlayThisSchemeCard(
        ).SetPlay(only_if_your_identity_has_trait="X-MEN"),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            new_recruits,
        ),
    ]

