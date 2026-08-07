from . import *

# Defend the Title

def GetAbilities() -> Sequence['Ability']:

    def defend_the_title_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        champion = FindTheChampion(effect)
        if champion:
            Faces.PlaceCountersOn([champion], 2, 'ratings', effect)

    def defend_the_title_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        magog = Worlds.FindCardOnField(
            effect,
            name="MaGog",
            card_type=Villain
        )
        if magog:
            def action(unit: Unit2, taken_damage: int):
                challenger = FindTheChallengers(effect)
                if challenger and \
                    Hero.IsType(unit) and \
                    taken_damage == 0:
                    Faces.PlaceCountersOn([challenger], 2, 'ratings', effect)

            magog.DoAttackYou(player, effect, if_unit_defends_against=action)


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            defend_the_title_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            defend_the_title_hero
        ),
    ]

