using System.Collections.Generic;
using UnityEngine;

namespace SteelOfStalin
{
    public class BattleController : MonoBehaviour
    {
        public GameObject Battle { get; set; }

        private void Start()
        {
            Battle = Resources.Load<GameObject>(@"Prefabs\battle");
            if (Battle.GetComponent<Battle>() == null)
            {
                Battle.AddComponent<Battle>();
            }
            Instantiate(Battle);
        }

        private void Update()
        {

        }
    }
}

