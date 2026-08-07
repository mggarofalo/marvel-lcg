from . import *

# A.I.M. Jailer

def GetAbilities() -> Sequence['Ability']:

    def aim_jailer_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        faces = Worlds.FindCardsOnField(effect, CardFinder2("RESCUED", Ally))
        face = Filter.One(faces, effect, fewest_remaining_hp=True)
        if face:
            this.BasicAttack([face], effect)
        else:
            player = message.GetToPlayer()
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        Faces.PlaceCountersOn(targets, 1, 'lock', effect)
                ).SetTarget(name="Holding Cell")
            )

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            aim_jailer_revealed
        ),
    ]

