from . import *

# * Calvin Zabo

def GetAbilities() -> Sequence['Ability']:

    def calvin_zabo_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        face = Worlds.FindCardOnField(
            effect,
            name="Mister Hyde",
            card_type=Minion
        )
        if face and Faces.DiscardAll([this], effect):
            face.DoAttackYou(player, effect, property=AttackProperty(additional_value=2, overkill=True))


    def calvin_zabo_defeated(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = this.GetEngagedPlayer()
        face = Search.EncounterCard(
            effect,
            include_discard_pile=True,
            name="Mister Hyde",
            card_type=Minion
        )
        if face:
            face.PutIntoPlay(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            calvin_zabo_revealed
        ),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            calvin_zabo_defeated
        ),
    ]

