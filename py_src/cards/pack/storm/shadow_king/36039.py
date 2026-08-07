from . import *

# Astral Attack

def GetAbilities() -> Sequence['Ability']:

    def astral_attack_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        if not Worlds.GetOnFieldMinions(effect, CardFinder2("CONTROLLED")):
            ThisCardGainSurge(effect)
        else:
            Worlds.Enemies.AllMinionActivateAgainstYou(
                effect,
                player,
                trait="CONTROLLED",
            )

    def astral_attack_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        faces = Worlds.GetEncounterDiscardPile(effect).FindCards(set_name="Shadow King")
        Faces.ShuffleAllTo(faces, "EncounterDeck", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            astral_attack_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            astral_attack_boost
        ),
    ]

