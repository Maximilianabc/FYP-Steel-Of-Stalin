using SteelOfStalin.DataIO;
using System;
using System.Collections;
using UnityEngine;

namespace SteelOfStalin
{
    public class MenuController : MonoBehaviour
    {
        private void Start()
        {
            if (GameObject.Find("game") == null)
            {
                GameObject game = Resources.Load<GameObject>(@"Prefabs\game");
                if (game.GetComponent<Game>() == null)
                {
                    game.AddComponent<Game>();
                }
                GameObject gameInstance = Instantiate(game);
                gameInstance.name = "game";
                DontDestroyOnLoad(gameInstance);
            }

            if (GameObject.Find("network") == null)
            {
                GameObject network = Resources.Load<GameObject>(@"Prefabs\network");
                GameObject network_instance = Instantiate(network);
                network_instance.name = "network";
                DontDestroyOnLoad(network_instance);
            }


            if (GameObject.Find("UI_util") == null)
            {
                GameObject UI_util = Resources.Load<GameObject>(@"Prefabs\UI_util");
                GameObject UI_util_instance = Instantiate(UI_util);
                UI_util_instance.name = "UI_util";
                DontDestroyOnLoad(UI_util_instance);
            }

            _ = StartCoroutine(ReloadBattleRelatedObjects(() => Destroy(gameObject)));
        }

        private IEnumerator ReloadBattleRelatedObjects(Action action)
        {
            yield return new WaitWhile(() => GameObject.Find("game") == null);
            yield return new WaitWhile(() => !Game.NeedReloadBattleObjects);

            if (GameObject.Find("network_util") == null)
            {
                GameObject network_util = Resources.Load<GameObject>(@"Prefabs\network_util");
                GameObject network_util_instance = Instantiate(network_util);
                network_util_instance.name = "network_util";
                if (network_util_instance.GetComponent<NetworkUtilities>() == null)
                {
                    network_util_instance.AddComponent<NetworkUtilities>();
                }
                DontDestroyOnLoad(network_util_instance);
            }

            if (GameObject.Find("battle") == null)
            {
                GameObject battle = Resources.Load<GameObject>(@"Prefabs\battle");
                GameObject battle_instance = Instantiate(battle);
                if (battle_instance.GetComponent<Battle>() == null)
                {
                    battle_instance.AddComponent<Battle>();
                }
                battle_instance.name = "battle";
                DontDestroyOnLoad(battle_instance);
            }

            yield return new WaitWhile(() => string.IsNullOrEmpty(Game.Profile.Name));
            if (GameObject.Find(Game.Profile.Name) == null)
            {
                GameObject player = Resources.Load<GameObject>(@"Prefabs\player");
                GameObject player_instance = Instantiate(player);
                if (player_instance.GetComponent<PlayerObject>() == null)
                {
                    player_instance.AddComponent<PlayerObject>();
                }
                DontDestroyOnLoad(player_instance);
            }
            yield return null;
        }
    }
}
