from . import *

# Troll

def GetAbilities() -> Sequence['Ability']:

    def troll_defeated(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetKillerPlayer()
        if player:
            faces = player.discard_pile.Get()
            faces = Filter.ByType(faces, Ally)
            faces = Filter.All(faces, highest_cost=True)
            face = player.MayChooseFace(faces, effect)
            if face:
                face.PutIntoPlay(player, effect)

    return [
        AbilityFactory.UnitTakeAdditionalDamageFromCards(
            "This",
            +1,
            CardFinder(has_printed_res="B"),
        ),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            troll_defeated
        )
    ]

