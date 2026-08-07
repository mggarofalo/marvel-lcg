from . import *

# Stalked by Sabretooth 1B

def GetAbilities() -> Sequence['Ability']:

    def stalked_by_sabretooth(effect: 'Effect', message: 'Message.AfterResolveVillainPhaseStep') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        ally = Worlds.FindCardOnField(
            effect,
            ROBERT_KELLY_FINDER
        )
        if ally:
            if this.threat >= 6 * Worlds.GetPlayerNumIcon(effect):
                damage = 3
            else:
                damage = 2
            this.DealDamage([ally], damage, effect)

    return [
        AbilityFactory.AfterResolveVillainPhaseStep(
            AbilityType.ForcedResponse,
            1,
            stalked_by_sabretooth
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            ROBERT_KELLY_FINDER,
            treat_as_blank=True,
        ),
        # While Robert Kelly is attached to Find the Senator, treat his text box as if it were blank.
        IfRobertKellyLeavesPlayThePlayersLoseTheGame()
    ]

