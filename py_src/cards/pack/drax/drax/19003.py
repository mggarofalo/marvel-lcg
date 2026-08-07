from . import *

# "Fight Me, Coward!"

def GetAbilities() -> Sequence['Ability']:

    def fight_me_coward(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        Faces.ReadyAll(effect.targets, effect)
        initiator.DrawUp(1, effect)
        villain = Worlds.FindVillain(effect)
        if villain:
            villain.DoAttackYou(initiator, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            fight_me_coward
        ).SetPlay().SetLabel()
        .SetTarget("YourHero")
    ]

