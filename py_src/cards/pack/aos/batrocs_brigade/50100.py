from . import *

# * Zaran

def GetAbilities() -> Sequence['Ability']:

    def zaran(effect: 'Effect', ui: List['CardFace']) -> int:
        this = effect.this.CastTo(Minion)
        Unused(this)

        faces = this.GetPlacedCardArea().GetAll()
        ui.extend(faces)
        return FacesCounter.GetPrintedResourceCost(faces)

    def zaran_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        face = Filter.One(player.GetControlCardsByType(CardFinder2("WEAPON", Upgrade), upgrade=True), effect)
        if not face:
            face = player.player_deck.GetTop()

        if face:
            this.TuckCardUnderHere(face, effect, peek=True)

    return [
        AbilityFactory.ThisGainKeyword(
            zaran,
            attack=1,
            change_on_event=OnEvent.TuckUnder("Here")
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            zaran_revealed
        ),
    ]

