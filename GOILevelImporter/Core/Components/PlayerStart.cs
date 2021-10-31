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
            if (!PlayerPrefs.HasKey("SaveGame0") || !PlayerPrefs.HasKey("SaveGame1"))
            {
                ComponentHelper.Instance.Teleport(transform.position);
            }

            Destroy(gameObject);
        }
    }
}
