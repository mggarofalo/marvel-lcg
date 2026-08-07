from . import *

# Pale Little Spider

def GetAbilities() -> Sequence['Ability']:

    def pale_little_spider_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        def action():
            ThisCardGainSurge(effect)

        face = Find.FindAndReveal(
            effect,
            player,
            name="Black Widow",
            card_type=Enemy
        )
        if face:
            face.DoActivate(player, effect, if_no_activate=action)
        else:
            action()


    def pale_little_spider_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if identity.IsConfused():
            this.PlaceThreatOnSchemes("MainScheme", 1, effect)
        else:
            Faces.GiveStatus([identity], "Confused", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            pale_little_spider_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            pale_little_spider_boost
        ),
    ]

