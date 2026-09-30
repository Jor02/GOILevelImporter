using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace GOILevelImporter.Components
{
    [MovedFrom(false, "Assembly-CSharp", "GOILevelImporter.Components", "PlayerStart")]
    public class PlayerStart : MonoBehaviour
    {
        private void Start()
        {
            if (!PlayerPrefs.HasKey("SaveGame0") || !PlayerPrefs.HasKey("SaveGame1"))
            {
                ComponentHelper.Instance.Teleport(transform.position);
            }

            Destroy(gameObject);
        }
    }
}
