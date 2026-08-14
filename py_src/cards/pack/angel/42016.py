from . import *

# Taunt

def GetAbilities() -> Sequence['Ability']:

    def taunt(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        villain = Worlds.FindVillain(effect)
        if villain:
            villain.DoAttackYou(initiator, effect, other_characters_cannot_defend=True)

        initiator.DrawUp(3, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            taunt
        ).SetPlay().SetLabel(),
    ]

