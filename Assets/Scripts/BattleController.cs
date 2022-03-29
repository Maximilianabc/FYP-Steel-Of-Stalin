using SteelOfStalin.Assets.Props;
using UnityEngine;

namespace SteelOfStalin
{
    public class BattleController : MonoBehaviour
    {
        private void Start()
        {
            Game.LoadAllAssets(); // TODO remove it later, testing purpose only
            GameObject Battle = Game.GameObjects.Find(g => g.name == "battle");

            GameObject battleInstance = Instantiate(Battle);
            if (battleInstance.GetComponent<Battle>() == null)
            {
                battleInstance.AddComponent<Battle>();
            }
            battleInstance.name = "battle";
            Destroy(gameObject);
        }

        private void Update()
        {

        }
    }
}

