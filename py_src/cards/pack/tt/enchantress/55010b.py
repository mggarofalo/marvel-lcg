from . import *

# Trance of Sloth

def GetAbilities() -> Sequence['Ability']:

    def trance_of_sloth_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if not Faces.GiveStatus([identity], "Stunned", effect):
            this.PlaceThreatOnSchemes("MainScheme", 2, effect)

    def trance_of_sloth(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()

        this.HealthUnits(effect.targets, 1, effect)
        player.DrawUp(1, effect)
        player.DiscardHandCards((1, 1), effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            trait="ENTHRALLED"
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            trance_of_sloth_revealed
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.ForcedAction,
            trance_of_sloth
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(name="Enchantress", canbe_heal=True)
        .SetTarget2("This")
    ]

