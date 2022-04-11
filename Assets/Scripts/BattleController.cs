using SteelOfStalin.Assets.Props;
using SteelOfStalin.DataIO;
using Unity.Netcode;
using UnityEngine;

namespace SteelOfStalin
{
    public class BattleController : MonoBehaviour
    {
        public bool StartAsHost;
        private void Start()
        {
            // assets aren't loaded if during testing (i.e. calling from tests or directly play battle scene)
            if (!Game.AssetsLoaded)
            {
                Game.LoadAllAssets();
            }

            GameObject battle = Game.GameObjects.Find(g => g.name == "battle");
            GameObject battle_instance = Instantiate(battle);
            if (battle_instance.GetComponent<Battle>() == null)
            {
                battle_instance.AddComponent<Battle>();
            }
            battle_instance.name = "battle";

            if (NetworkManager.Singleton == null)
            {
                // must be playing battle scene directly, add a decoy network prefab first
                GameObject network = Game.GameObjects.Find(g => g.name == "network");
                GameObject network_instance = Instantiate(network);
                network_instance.name = "network";
                DontDestroyOnLoad(network_instance);

                GameObject network_util = Game.GameObjects.Find(g => g.name == "network_util");
                GameObject network_util_instance = Instantiate(network_util);
                if (network_util_instance.GetComponent<NetworkUtilities>() == null)
                {
                    network_util_instance.AddComponent<NetworkUtilities>();
                }
                network_util_instance.name = "network_util";
                DontDestroyOnLoad(network_util_instance);

                if (StartAsHost)
                {
                    NetworkManager.Singleton.StartHost();
                }
                else
                {
                    NetworkManager.Singleton.StartClient();
                }
            }
            Destroy(gameObject);
        }

        private void Update()
        {

        }
    }
}

