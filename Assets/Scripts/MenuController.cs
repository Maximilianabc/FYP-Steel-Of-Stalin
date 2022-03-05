using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SteelOfStalin
{
    public class MenuController : MonoBehaviour
    {
        public GameObject Game { get; set; }
        void Start()
        {
            Game = Resources.Load<GameObject>(@"Prefabs\game");
            if (Game.GetComponent<Game>() == null)
            {
                Game.AddComponent<Game>();
            }
            Instantiate(Game);
        }

        void Update()
        {

        }
    }
}
