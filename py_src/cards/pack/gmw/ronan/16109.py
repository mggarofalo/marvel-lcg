from . import *

# * Universal Weapon

def GetAbilities() -> Sequence['Ability']:

    def universal_weapon(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        encounter_deck = Worlds.GetEncounterDeck(effect)
        Faces.ShuffleAllTo([this], encounter_deck, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Ronan the Accuser"),
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            universal_weapon
        ).SetCostFunc(CostFunc.TakeDamage(2, "YourIdentity"))
        .SetCostFunc(CostFunc.DealPlayerEncounterCard(
            1, "Initiator")),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Ronan the Accuser"),
            stalwart=1,
        ),
        AbilityFactory.WhenThisBoostAttachTo(
            CardFinder(name="Ronan the Accuser")
        ),
    ]

