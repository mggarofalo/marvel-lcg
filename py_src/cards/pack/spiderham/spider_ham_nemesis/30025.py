from . import *

# Nefarious Trap

def GetAbilities() -> Sequence['Ability']:

    def nefarious_trap(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.killer.GetControlBy()
        if Player.IsType(player):
            minion = Worlds.FindCardOnField(
                effect,
                name="The Green Gobbler",
                card_type=Enemy
            )
            if minion:
                minion.DoAttackYou(player, effect)
            else:
                face = Search.EncounterCard(
                    effect,
                    include_discard_pile=True,
                    name="The Green Gobbler",
                    card_type=Minion
                )
                if face:
                    face.PutIntoPlay(player, effect)

    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            nefarious_trap
        )
    ]

