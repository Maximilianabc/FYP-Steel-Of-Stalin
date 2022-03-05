using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SteelOfStalin
{
    public class BattleController : MonoBehaviour
    {
        public GameObject Battle { get; set; }
        void Start()
        {
            Battle = Resources.Load<GameObject>(@"Prefabs\battle");
            Battle.AddComponent<Battle>();
            Instantiate(Battle);
        }

        void Update()
        {

        }
    }
}

