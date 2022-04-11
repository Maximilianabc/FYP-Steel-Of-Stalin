using SteelOfStalin.DataIO;
using UnityEngine;

namespace SteelOfStalin
{
    public class MenuController : MonoBehaviour
    {
        private void Start()
        {
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
            if (network_util_instance.GetComponent<NetworkUtilities>() == null)
            {
                network_util_instance.AddComponent<NetworkUtilities>();
            }
            DontDestroyOnLoad(network_util_instance);

            Destroy(gameObject);
        }

        private void Update()
        {

        }
    }
}
