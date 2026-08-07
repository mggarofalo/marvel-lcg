from . import *

# Now or Never

def GetAbilities() -> Sequence['Ability']:

    def now_or_never_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        scheme = Worlds.FindMainScheme(this)
        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Place 1 acceleration token on the main scheme",
                lambda targets:
                    scheme.PlaceAccelerationToken(1, effect)
            ) if scheme != None else None,
            AbilityFactory.ForChoiceAbilityWithCost(
                Cost("1"),
                "Exhause a character you control and spend 1 resource of any type",
                lambda targets, res:
                    Faces.ExhaustAll(targets, effect)
            ).SetTarget("YouControlUnit", canbe_exhaust=True)
        )


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            now_or_never_revealed
        ),
    ]

