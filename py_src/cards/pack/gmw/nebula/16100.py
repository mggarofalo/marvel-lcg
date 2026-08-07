from . import *

# Barrel Roll

def GetAbilities() -> Sequence['Ability']:

    def barrel_roll_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed|Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        ship = FindNebulaShip(effect)
        if ship:
            Faces.PlaceCountersOn([ship], 1, 'evasion',  effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            barrel_roll_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            barrel_roll_revealed
        ),
    ]

