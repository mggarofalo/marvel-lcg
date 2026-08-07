from . import *

# Trance of Envy

def GetAbilities() -> Sequence['Ability']:

    def trance_of_envy_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        if not player.DiscardControlCards(effect):
            player.DiscardDeckTopCards(8, effect)

    def trance_of_envy(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        Faces.GiveFacedownBoostCards(effect.targets, 1, effect)
        Faces.ReadyAll(effect.targets2, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            trait="ENTHRALLED"
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            trance_of_envy_revealed
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.ForcedAction,
            trance_of_envy
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(name="Enchantress")
        .SetTarget2("YourIdentity", canbe_ready=True),
    ]

