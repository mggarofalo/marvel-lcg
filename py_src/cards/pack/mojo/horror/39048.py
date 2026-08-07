from . import *

# Bandolier of Stakes

def GetAbilities() -> Sequence['Ability']:

    def bandolier_of_stakes_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        if player.AskSpendResources(Cost("1"), effect):
            this.AttachTo2(player.GetIdentity(), effect)
        else:
            Faces.DiscardAll([this], effect)


    def bandolier_of_stakes(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.GainATKForThisAttack(1, effect)
        message.GainPiercing(effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            bandolier_of_stakes_revealed
        ),
        AbilityFactory.WhenUnitMakeAttack(
            AbilityType.HeroInterrupt,
            "YourHero",
            bandolier_of_stakes,
            is_basic_attack=True,
        ).SetCostFunc(CostFunc.Counter("This", 1, 'stake')),
    ]

