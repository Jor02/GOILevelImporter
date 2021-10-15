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
            ComponentHelper.Instance.Teleport(transform.position);
            Destroy(gameObject);
        }
    }
}
