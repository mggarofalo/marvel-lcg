from . import *

# Kang's Dominion

def GetAbilities() -> Sequence['Ability']:

    def kangs_dominion(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()
        player.DealEncounterCards(1, effect)


    return [
        AbilityFactory.UnitCannotTakeDamageWhile(
            AbilityType.NonKeyword,
            CardFinder(name="Kang"),
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            kangs_dominion,
            has_defeating_player=True
        )
    ]

