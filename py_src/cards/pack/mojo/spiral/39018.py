from . import *

# Spiral's Swords

def GetAbilities() -> Sequence['Ability']:

    def spirals_swords(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        Faces.RemoveCountersOn([this], 1, 'sword', effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Spiral")
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Spiral"),
            get_new_value=lambda effect, face, ui:
                effect.this.CastTo(Attachment).GetCounters('sword'),
            attack=1,
            ex_change_on_event=OnEvent.Counter("This", 'sword')
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            spirals_swords,
            conditions=[
                lambda effect, message:
                    Worlds.FindCardOnField(
                        effect,
                        name="Spiral",
                        trait="CORNERED"
                        ) != None
            ],
        ).SetCost(Cost("RR")),
    ]

