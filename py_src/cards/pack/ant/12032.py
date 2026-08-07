from . import *

# Muster Courage

def GetAbilities() -> Sequence['Ability']:

    def muster_courage(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Tough", effect)

    def get_villain_stage(effect: 'Effect'):
        villain = Worlds.FindVillain(effect)
        if villain:
            return villain.printed_stage
        return 0

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            muster_courage
        ).SetPlay(only_if_your_identity_has_trait="AVENGER").SetLabel()
        .SetTarget(Friend, canbe_tough=True, range=(1, get_villain_stage)),
    ]

