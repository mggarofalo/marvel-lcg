from . import *

# Stryfe's Grasp

def GetAbilities() -> Sequence['Ability']:

    def stryfes_grasp(effect: 'Effect', message: 'Message.AfterUnitBeDefeated|Message.AfterSchemeRemoveThreat') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        # threat = this.threat

        this.card.Flip(effect)

        # Threat will not be removed after flip
        # face = Worlds.FindCardOnField(
        #     effect,
        #     name="Living Bomb",
        #     card_type=SchemeSide2
        # )

        # if face:
        #     this.PlaceThreatOnSchemes([face], threat, effect)


    return [
        *AbilityFactory.UnitCannotAttackTarget(
            CardFinder(name="Hope Summers"),
            can_only_attack=CardFinder(name="Stryfe"),
        ),
        *AbilityFactory.UnitCannotThwartTarget(
            CardFinder(name="Hope Summers"),
            can_only_thwart="This",
        ),
        AbilityFactory.AfterUnitBeDefeated(
            AbilityType.ForcedResponse,
            CardFinder(name="Stryfe"),
            stryfes_grasp
        ),
        AbilityFactory.AfterSchemeRemoveThreat(
            AbilityType.ForcedResponse,
            "This",
            stryfes_grasp,
            last_threat=True,
        ),
    ]

