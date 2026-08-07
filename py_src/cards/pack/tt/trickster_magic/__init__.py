from cards.pack import *

def ThePlayerWhoDefeatedThisMinionPutsTheSetAsideAllyIntoPlayUnderTheirControl(name: str):
    def absorbing_man_defeated(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetDefeatingPlayer()

        faces = Worlds.GetSetAsideAreaCards(effect, CardFinder(name=name, card_type=Ally))
        if faces:
            faces[0].PutIntoPlay(player, effect, under_control=True)

    return AbilityFactory.WhenUnitBeDefeated(
        AbilityType.WhenDefeated,
        "This",
        absorbing_man_defeated
    )
