from . import *

# * Absorbing Man

def GetAbilities() -> Sequence['Ability']:

    def absorbing_man(effect: 'Effect', ui: List['CardFace']) -> int:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = this.engaged_player
        if player:
            faces = Filter.All(player.GetControlCards(), HasCost, highest_cost=True)
            if faces:
                face = faces[0]
                ui.append(face)
                return face.printed_cost.val
        return 0

    return [
        AbilityFactory.ThisGainKeyword(
            absorbing_man,
            attack=1,
            change_on_event=OnEvent.CardInPlay(None)
        ),
        ThePlayerWhoDefeatedThisMinionPutsTheSetAsideAllyIntoPlayUnderTheirControl("Absorbing Man")
    ]

