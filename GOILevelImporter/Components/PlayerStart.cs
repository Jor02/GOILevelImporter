using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GOILevelImporter.Components
{
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
