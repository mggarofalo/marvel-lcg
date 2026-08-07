from . import *

# * Show of Empathy

def GetAbilities() -> Sequence['Ability']:

    def show_of_empathy(effect: 'Effect', message: 'Message.AfterCardRemovedToken') -> None:
        this = effect.this.CastTo(PlayerSideScheme)
        Unused(this)

        player = this.GetOwnerPlayer()

        size = message.removed_num
        Faces.PlaceTokensOn(effect.targets, size, 'threat', effect)

        for face in effect.targets:
            minion = face.CastTo(Minion)
            if minion.GetTokens('threat') >= minion.health:
                Faces.AddToVictoryDisplay([this], effect)
                attach = Search.SearchForCard(
                    effect,
                    player,
                    include_set_aside=True,
                    name="Redemption",
                    card_type=Upgrade
                )
                if attach:
                    attach.card.SetOwner(player)
                    attach.AttachTo2(minion, effect)
                break

    return [
        AbilityFactory.WhenThreatIsRemovedFrom(
            AbilityType.ForcedInterrupt,
            "This",
            show_of_empathy,
        ).SetTarget(Minion, non_trait="ELITE")
    ]

