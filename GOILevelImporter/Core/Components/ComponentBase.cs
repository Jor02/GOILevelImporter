using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GOILevelImporter.Core.Components
{
    abstract class ComponentBase : MonoBehaviour
    {
        internal bool canStart { get; private set; } = false;

        public void StartComponent()
        {
            StartComp();
            canStart = true;
        }

        protected abstract void StartComp();
    }
}
