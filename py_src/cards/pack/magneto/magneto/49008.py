from . import *

# Electromagnetic Blast

def GetAbilities() -> Sequence['Ability']:

    def electromagnetic_blast(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        def action():
            Players.DiscardHeroActionAttachment(initiator, None, effect, may=True)
        this.RemoveThreatFromSchemes(effect.targets, 3, effect, if_this_remove_last_threat=action)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            electromagnetic_blast
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

