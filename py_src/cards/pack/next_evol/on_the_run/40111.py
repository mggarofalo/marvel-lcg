from . import *

# Tag Team

def GetAbilities() -> Sequence['Ability']:

    def tag_team_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        def action(targets: Sequence['CardFace']):
            for target in targets:
                # Worlds.Enemies.DoActivateAgainstYou
                if Minion.IsType(target):
                    target.DoActivate(player, effect)

        def action2(targets: Sequence['CardFace']):
            Worlds.DiscardEncounterCards(7, effect)
            face = Worlds.GetEncounterDiscardPile(effect).FindTopmost(CardFinder2("MARAUDER", Minion))
            if face:
                face.PutIntoPlay(player, effect)


        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Each [[MARAUDER]] minion engaged with you activates against you",
                action
            ).SetTarget("YourEngagedMinions", trait="MARAUDER", range="All"),
            AbilityFactory.ForChoiceAbility(
                "Discard 7 cards from the top of the encounter deck. Put the topmost [[MARAUDER]] minion in the encounter discard pile into play engaged with you",
                action2
            )
        )

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            tag_team_revealed
        ),
    ]

