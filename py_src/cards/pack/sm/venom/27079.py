from . import *

# Guard the Bell Tower

def GetAbilities() -> Sequence['Ability']:

    def guard_the_bell_tower_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        bell_tower = FindBellTower(effect)
        if bell_tower:
            bell_tower.RemoveAllCounters(effect)
            if not bell_tower.HasTrait("QUIET"):
                bell_tower.card.Flip(effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(name="Bell Tower"),
            treat_as_blank=True,
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            guard_the_bell_tower_revealed
        ),
    ]

