from . import *

# The Wreckoning

def GetAbilities() -> Sequence['Ability']:

    def the_wreckoning(effect: 'Effect', message: 'Message.WhenGameWouldBegin'):
        this = effect.this.CastTo(Challenge)
        Unused(this)

        for name in ["Magic Crowbar", "Ball and Chain", "Distracting Taunts", "Bulldozer's Helmet"]:
            SetupCards.Reveal(
                effect,
                name=name,
            )

    def the_wreckoning_value(effect: 'Effect', face: 'CardFace'):
        this = effect.this.CastTo(Challenge)
        Unused(this)

        return len(Worlds.GetDefeatedVillain(effect))

    return [
        AbilityFactory.AfterGameSetup(
            the_wreckoning
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Villain,
            attack=1,
            scheme=1,
            get_new_value=the_wreckoning_value,
            change_on_event=OnEvent.LeavePlayAfter(Villain)
        ),
    ]

