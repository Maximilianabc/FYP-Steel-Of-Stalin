using SteelOfStalin.DataIO;
using UnityEngine;

namespace SteelOfStalin
{
    public class MenuController : MonoBehaviour
    {
        private void Start()
        {
            if (GameObject.Find("game") != null) return;
            GameObject game = Resources.Load<GameObject>(@"Prefabs\game");
            if (game.GetComponent<Game>() == null)
            {
                game.AddComponent<Game>();
            }
            GameObject gameInstance = Instantiate(game);
            gameInstance.name = "game";
            DontDestroyOnLoad(gameInstance);

            GameObject network = Resources.Load<GameObject>(@"Prefabs\network");
            GameObject network_instance = Instantiate(network);
            network_instance.name = "network";
            DontDestroyOnLoad(network_instance);

            GameObject network_util = Resources.Load<GameObject>(@"Prefabs\network_util");
            GameObject network_util_instance = Instantiate(network_util);
            network_util_instance.name = "network_util";
            if (network_util_instance.GetComponent<NetworkUtilities>() == null)
            {
                network_util_instance.AddComponent<NetworkUtilities>();
            }
            DontDestroyOnLoad(network_util_instance);

            GameObject UI_util = Resources.Load<GameObject>(@"Prefabs\UI_util");
            GameObject UI_util_instance = Instantiate(UI_util);
            UI_util_instance.name = "UI_util";
            DontDestroyOnLoad(UI_util_instance);

            GameObject battle = Resources.Load<GameObject>(@"Prefabs\battle");
            GameObject battle_instance = Instantiate(battle);
            if (battle_instance.GetComponent<Battle>() == null)
            {
                battle_instance.AddComponent<Battle>();
            }
            battle_instance.name = "battle";
            DontDestroyOnLoad(battle_instance);

            GameObject player = Resources.Load<GameObject>(@"Prefabs\player");
            GameObject player_instance = Instantiate(player);
            if (player_instance.GetComponent<PlayerObject>() == null)
            {
                player_instance.AddComponent<PlayerObject>();
            }
            DontDestroyOnLoad(player_instance);

            Destroy(gameObject);
        }

        private void Update()
        {

        }
    }
}
