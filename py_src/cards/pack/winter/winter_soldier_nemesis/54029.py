from . import *

# Hydra Hit Squad

def GetAbilities() -> Sequence['Ability']:

    def hydra_hit_squad(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()

        face = Search.EncounterCard(
            effect,
            player,
            include_discard_pile=True,
            trait="HYDRA",
            card_type=Minion
        )
        if face:
            face.Reveal(player, effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("HYDRA", Minion),
            attack=1,
            health=2,
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            hydra_hit_squad,
        ),
    ]

