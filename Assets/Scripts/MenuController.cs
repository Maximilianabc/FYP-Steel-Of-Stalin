using UnityEngine;

namespace SteelOfStalin
{
    public class MenuController : MonoBehaviour
    {
        public GameObject Game { get; set; }

        private void Start()
        {
            Game = Resources.Load<GameObject>(@"Prefabs\game");
            if (Game.GetComponent<Game>() == null)
            {
                Game.AddComponent<Game>();
            }
            GameObject gameInstance = Instantiate(Game);
            gameInstance.name = "game";
            DontDestroyOnLoad(gameInstance);
            Destroy(gameObject);
        }

        private void Update()
        {

        }
    }
}
