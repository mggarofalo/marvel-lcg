from . import *

# Universal Church of Truth

def GetAbilities() -> Sequence['Ability']:

    def universal_church_of_truth(effect: 'Effect', message: 'Message.AfterDeckReset') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.deck.GetOwner()
        if isinstance(player, Player):
            identity = player.GetIdentity()
            Faces.ExhaustAll([identity], effect)
            Faces.GiveStatus([identity], "Stunned", effect)

    return [
        AbilityFactory.AfterDeckReset(
            AbilityType.ForcedResponse,
            None,
            universal_church_of_truth,
            is_player_deck=True
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            RevealThisCard
        ),
    ]

