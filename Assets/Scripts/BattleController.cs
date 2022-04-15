using SteelOfStalin.DataIO;
using UnityEngine;
using Unity.Netcode;
#if UNITY_EDITOR
using ParrelSync;
#endif

namespace SteelOfStalin
{
    public class BattleController : MonoBehaviour
    {
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

            GameObject player = Game.GameObjects.Find(g => g.name == "player");
            GameObject player_instance = Instantiate(player);
            if (player_instance.GetComponent<PlayerObject>() == null)
            {
                player_instance.AddComponent<PlayerObject>();
            }

#if UNITY_EDITOR
            if (Game.Network == null)
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

                if (Game.ActiveBattle == null)
                {
                    Game.ActiveBattle = new BattleInfo();
                }
                if (Game.Profile == null || string.IsNullOrEmpty(Game.Profile.Name))
                {
                    string name = ClonesManager.IsClone() ? $"dummy_client_{new System.Random().Next()}" : "dummy_host";
                    Game.Profile = new PlayerProfile() { Name = name };
                }

                Game.Network.ConnectionApprovalCallback += Game.ApprovalCheck;
                if (!ClonesManager.IsClone())
                {
                    Game.StartHost();
                }
                else
                {
                    Game.StartClient();
                }
            }
#endif
            Destroy(gameObject);
        }
    }
}

