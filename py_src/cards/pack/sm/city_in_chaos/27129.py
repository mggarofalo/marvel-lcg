from . import *

# Calling in Favors

def GetAbilities() -> Sequence['Ability']:

    def calling_in_favors_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        enemy = Worlds.FindCardOnField(
            effect,
            name="Rhino",
            card_type=Enemy
        )
        if enemy:
            enemy.DoSchemes(player, effect, property=SchemeProperty(additional_value=2))
        else:
            face = Search.EncounterCard(
                effect,
                include_discard_pile=True,
                name="Rhino",
                card_type=Minion
            )
            if face:
                face.PutIntoPlay(player, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            calling_in_favors_revealed
        ),
    ]

