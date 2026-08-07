from . import *

# Tap In

def GetAbilities() -> Sequence['Ability']:

    def tap_in_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        minions = Worlds.GetOnFieldMinions(effect, CardFinder(not_engaged_with=player, trait="THUNDERBOLT"))
        minion = Filter.One(minions, effect, least_damage=True)

        def action():
            face = Worlds.FindCardOnField(effect, name="Citizen V", card_type=Enemy)
            if face:
                face.DoActivate(player, effect)

        if minion:
            minion.EngagePlayer(player, effect)
            minion.DoActivate(player, effect, if_no_activate=action)
        else:
            action()

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            tap_in_revealed
        ),
    ]

