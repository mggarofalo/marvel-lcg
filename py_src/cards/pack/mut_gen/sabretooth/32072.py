from . import *

# Feral Rage

def GetAbilities() -> Sequence['Ability']:

    def feral_rage(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()

        villain = Worlds.FindCardOnField(
            effect,
            name="Sabretooth",
            card_type=Villain
        )
        if villain:
            villain.DoAttackYou(player, effect)

    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            feral_rage,
            has_defeating_player=True
        ),
    ]

