using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GOILevelImporter.Core.Components
{
    class PlayerStart : ComponentBase
    {
        protected override void StartComp()
        {
            GameObject player = GameObject.Find("/Player");
            player.transform.position = transform.position;

            Destroy(gameObject);
        }
    }
}
