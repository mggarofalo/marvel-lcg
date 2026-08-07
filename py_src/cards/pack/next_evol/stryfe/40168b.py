from . import *

# Living Bomb

def GetAbilities() -> Sequence['Ability']:

    def living_bomb_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        scheme = Worlds.FindMainScheme(effect)
        if scheme:
            scheme.Advance("2A", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            living_bomb_revealed
        ).SetCannotBeCancel(),
        *AbilityFactory.UnitCannotBeDefeatedWhile(
            AbilityType.NonKeyword,
            CardFinder(name="Stryfe"),
            change_on_event=OnEvent.NONE,
        ),
    ]

