from . import *

# Arrest Order

def GetAbilities() -> Sequence['Ability']:

    def arrest_order_defeated(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()

        face = Worlds.GetEncounterDiscardPile(effect).FindTopmost(CardFinder2("S.H.I.E.L.D", Minion))
        if face:
            player.DealEncounterCard(face, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            arrest_order_defeated,
        ),
    ]

