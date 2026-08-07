from . import *

# * Namora: Aquaria Neptunia

def GetAbilities() -> Sequence['Ability']:

    def namora(effect: 'Effect', ui: List['CardFace']) -> int:
        this = effect.this.CastTo(Ally)
        Unused(this)

        player = this.GetControlByPlayer()
        return len(player.GetControlAllies()) - 1


    return [
        AbilityFactory.ThisGainKeyword(
            namora,
            health=1,
            change_on_event=OnEvent.CardInPlay("Character")
        )
    ]

