from . import *

# Body Swapped

def GetAbilities() -> Sequence['Ability']:

    def body_swapped(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        upgrades = player.GetControlUpgrade()
        faces = CardFinder2("PSI-ENERGY", Upgrade).Checks(upgrades)
        for face in faces:
            face.FlipTo(effect, name="Psi-Katana")

        upgrades = player.GetControlUpgrade()
        faces = CardFinder2("PSI-ENERGY", Upgrade).Checks(upgrades)
        Faces.ExhaustAll(faces, effect)

    def body_swapped_action(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Obligation)
        Faces.DiscardAll([this], effect)

    return [
        AbilityFactory.YouCannotFlipCards(
            CardFinder(name="Psi-Katana", card_type=Upgrade)
        ),
        AbilityFactory.WhenThisObligationRevealed(
            body_swapped
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            body_swapped_action
        ).SetCostFunc(CostFunc.Discard("YourHandCards", trait="PSIONIC"))
    ]

