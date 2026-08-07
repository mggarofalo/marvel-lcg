from . import *

# Puncture Wound

def GetAbilities() -> Sequence['Ability']:

    def puncture_wound(effect: 'Effect', message: 'Message.AfterPhaseBegin') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        enemy = this.GetBindFace()

        Faces.DiscardAll([this], effect)
        this.DealDamage([enemy], 3, effect)

    def puncture_wound_check(effect: 'Effect', face: 'CardFace') -> bool:
        world = effect.world

        for unit, _, targets in world.stat.this_turn_made_attack:
            if unit.IsName("X-23") or unit.IsName("Honey Badger"):
                if face in targets:
                    return True
        return False

    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            CardFinder(
                card_type=Enemy,
                check_effect_fn=puncture_wound_check
            )
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Enemy,
            attack=-1,
        ),
        AbilityFactory.AfterPhaseBegin(
            AbilityType.ForcedResponse,
            "Player",
            puncture_wound
        ),
    ]

