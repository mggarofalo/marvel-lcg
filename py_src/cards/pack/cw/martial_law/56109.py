from . import *

# S.H.I.E.L.D. Soldier

def GetAbilities() -> Sequence['Ability']:

    def shield_soldier_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        face = Worlds.GetEncounterDiscardPile(effect).FindTopmost(CardFinder2("S.H.I.E.L.D", Minion))
        if face:
            face.PutIntoPlay(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            shield_soldier_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            PutThisIntoPlay
        ),
    ]

