from . import *

# * Spider-Woman

def GetAbilities() -> Sequence['Ability']:

    class Buff_04031a(Buff):
        @override
        def __init__(self) -> None:
            super().__init__()
            self.aspects: List['str'] = []

        @override
        def OnRoundEnd(self):
            super().OnRoundEnd()
            self.aspects.clear()
            self.SetUIText("")

        def Update(self, face: 'CardFace'):
            assert ClassCard.IsType(face)
            aspect = face.GetAspect()
            if aspect:
                self.aspects.append(aspect)
            self.SetUIText(",".join(item[:2] for item in self.aspects))

        def Check(self, face: 'CardFace') -> bool:
            if ClassCard.IsType(face):
                return face.GetAspect() not in self.aspects
            else:
                return False

    def superhuman_agility(effect: 'Effect', message: 'Message.WhenPlayerPlayCard') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        this.GainUntilRoundEnd(
            effect,
            thwart=+1,
            attack=+1,
            defense=+1,
        )

        played_face = message.played_face
        this.GetBuff(Buff_04031a).Update(played_face)

    return [
        AbilityFactory.WhenPlayerPlayCard(
            AbilityType.Interrupt,
            "You",
            CardFinder(card_class="Aspect"),
            superhuman_agility,
            conditions=[
                # limit once per round for each aspect.
                lambda effect, message:
                    effect.this.GetBuff(Buff_04031a).Check(message.played_face)
            ]
        ).SetName("Superhuman Agility"),
    ]

