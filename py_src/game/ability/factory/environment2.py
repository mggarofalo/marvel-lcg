from . import *

class AbilityFactoryEnvironment2:

    @staticmethod
    def EachGainPrintedKeywordsOfEachOther(
        give_to_target: 'CardFinder',
        of_each: 'CardFinder'
    ) -> List['Ability']:
        from game.operate.worlds import Worlds
        from game.ability.factory import AbilityFactory

        return [
            *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
                give_to_target,
                get_new_value=lambda effect, face:
                    sum(x.printed_guard for x in Worlds.GetOnFieldMinions(effect, of_each) if x != face),
                guard=1,
                change_on_event=OnEvent.Trait(of_each)
            ),
            *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
                give_to_target,
                get_new_value=lambda effect, face:
                    sum(x.printed_quickstrike for x in Worlds.GetOnFieldMinions(effect, of_each) if x != face),
                quickstrike=1,
                change_on_event=OnEvent.Trait(of_each)
            ),
            *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
                give_to_target,
                get_new_value=lambda effect, face:
                    sum(x.printed_patrol for x in Worlds.GetOnFieldMinions(effect, of_each) if x != face),
                patrol=1,
                change_on_event=OnEvent.Trait(of_each)
            ),
            *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
                give_to_target,
                get_new_value=lambda effect, face:
                    sum(x.printed_villainous for x in Worlds.GetOnFieldMinions(effect, of_each) if x != face),
                villainous=1,
                change_on_event=OnEvent.Trait(of_each)
            ),
            *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
                give_to_target,
                get_new_value=lambda effect, face:
                    sum(x.printed_incite for x in Worlds.GetOnFieldMinions(effect, of_each) if x != face),
                incite=1,
                change_on_event=OnEvent.Trait(of_each)
            ),
            *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
                give_to_target,
                get_new_value=lambda effect, face:
                    sum(x.printed_toughness for x in Worlds.GetOnFieldMinions(effect, of_each) if x != face),
                toughness=1,
                change_on_event=OnEvent.Trait(of_each)
            ),
            *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
                give_to_target,
                get_new_value=lambda effect, face:
                    [x.printed_teamwork for x in Worlds.GetOnFieldMinions(effect, of_each) if x.printed_teamwork],
                teamwork=1,
                change_on_event=OnEvent.Trait(of_each)
            ),
            *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
                give_to_target,
                get_new_value=lambda effect, face:
                    sum(x.printed_surge for x in Worlds.GetOnFieldMinions(effect, of_each) if x != face),
                surge=1,
                change_on_event=OnEvent.Trait(of_each)
            ),
            *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
                give_to_target,
                get_new_value=lambda effect, face:
                    sum(x.printed_steady for x in Worlds.GetOnFieldMinions(effect, of_each) if x != face),
                steady=1,
                change_on_event=OnEvent.Trait(of_each)
            ),
            *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
                give_to_target,
                get_new_value=lambda effect, face:
                    sum(x.printed_stalwart for x in Worlds.GetOnFieldMinions(effect, of_each) if x != face),
                stalwart=1,
                change_on_event=OnEvent.Trait(of_each)
            ),
            *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
                give_to_target,
                get_new_value=lambda effect, face:
                    sum(x.printed_retaliate for x in Worlds.GetOnFieldMinions(effect, of_each) if x != face),
                retaliate=1,
                change_on_event=OnEvent.Trait(of_each)
            ),
        ]

