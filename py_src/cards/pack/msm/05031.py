from . import *

# Concussive Blow

def GetAbilities() -> Sequence['Ability']:

    def concussive_blow(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Confused", effect)
        if effect.GetPaidResources().HasColor("R"):
            this.DealDamage(effect.targets, 3, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            concussive_blow
        ).SetPlay().SetLabel('attack')
        # No `canbe_confused=True`. The printed card has a second clause -- "deal
        # 3 damage to that enemy" -- so a STALWART enemy, or one that is already
        # confused, is still a target a player has reason to choose. Restricting
        # the target to enemies that can take the status made the card
        # unplayable against every STALWART villain. The engine reserves
        # `canbe_confused=True` for a target whose *only* payoff is the status
        # (01011 Spider-Woman, 37012 Dazzler, 42003 Adaptive Plumage's second
        # target).
        .SetTarget(Enemy),
    ]

