from . import *

# * Hope Summers

def GetAbilities() -> Sequence['Ability']:

    def hope_summers_atk(effect: 'Effect', ui: List['CardFace']) -> int:
        this = effect.this.CastTo(Ally)
        Unused(this)

        player = this.GetControlBy()
        if Player.IsType(player):
            if player.IsHero():
                hero = player.GetHero()
                ui.append(hero)
                return hero.attack
        return 0

    def hope_summers_thw(effect: 'Effect', ui: List['CardFace']) -> int:
        this = effect.this.CastTo(Ally)
        Unused(this)

        player = this.GetControlBy()
        if Player.IsType(player):
            if player.IsHero():
                hero = player.GetHero()
                ui.append(hero)
                return hero.thwart
        return 0

    def hope_summers(effect: 'Effect', message: 'Message2') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Worlds.SetGameOver(False, effect)

    return [
        AbilityFactory.FirstPlayerControlThis(),
        *AbilityFactory.ThisNotCountAllyLimit(),
        AbilityFactory.ThisGainKeyword(
            hope_summers_atk,
            attack=1,
            change_on_event=[OnEvent.CardKeyword("YourIdentity"), OnEvent.PlayAreaCard("This")]
        ),
        AbilityFactory.ThisGainKeyword(
            hope_summers_thw,
            thwart=1,
            change_on_event=[OnEvent.CardKeyword("YourIdentity"), OnEvent.PlayAreaCard("This")]
        ),
        AbilityFactory.AfterCardLeavePlay(
            AbilityType.NonKeywordBold,
            "This",
            hope_summers
        )
    ]

