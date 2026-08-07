from . import *

# Promised Prosperity

def GetAbilities() -> Sequence['Ability']:

    def promised_prosperity_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        no_dealt: Set[Player] = set()

        def action(player: 'Player'):
            no_dealt.add(player)

            def set_dealt():
                if player in no_dealt:
                    no_dealt.remove(player)

            dealt_effects = this.effect.Registers(
                AbilityFactory.AfterPlayerDealEncounterCard(
                    AbilityType.Temp0,
                    lambda effect, message:
                        set_dealt(),
                    conditions=[
                        lambda dealt_effect, dealt_message:
                            player == dealt_message.GetToPlayer() and \
                            isinstance(dealt_message.by_effect.bind_message, Message.WhenResolveSpecialAbility) and \
                            effect == dealt_message.by_effect.bind_message.by_effect
                    ],
                )
            )
            ResolveFoulPlayerAbility(player, effect)
            Effects.UnRegister(dealt_effects)

        Players.ForEachPlayer(effect, action)

        if no_dealt:
            this.PlaceThreatOnSchemes([this], 2 * len(no_dealt), effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            promised_prosperity_revealed
        ),
    ]

