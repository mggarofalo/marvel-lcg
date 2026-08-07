from . import *

# Chance Encounter

def GetAbilities() -> Sequence['Ability']:

    def chance_encounter(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            card_type=Ally,
        )
        if face:
            initiator.GainCard(face, effect)

    return [
        AbilityFactory.CanPlayThisUpgradeCard(SchemeSide2),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.Interrupt,
            "AttachedSideScheme",
            chance_encounter
        ),
    ]

