from . import *

# Abduction Protocols

def GetAbilities() -> Sequence['Ability']:

    def abduction_protocols(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()

        allies = Worlds.AsideDeck(effect).FindCards(card_type=Ally, trait="CAPTIVE")
        ally = Rand.RandomChoice(allies, effect)
        ally.PutIntoPlay(player, effect, under_control=True)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            abduction_protocols,
            has_defeating_player=True
        ),
    ]

