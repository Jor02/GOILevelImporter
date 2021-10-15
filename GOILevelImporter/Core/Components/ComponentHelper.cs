using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GOILevelImporter.Core.Components
{
	class ComponentHelper : MonoBehaviour
	{
		public static ComponentHelper Instance { get; private set; }
		public GameObject player { get; }
		public Transform cursor { get; }
		public Transform camera { get; }

		public ComponentHelper()
		{
			Instance = this;
			player = GameObject.Find("/Player");
			cursor = GameObject.Find("/Cursor").transform;
			camera = GameObject.Find("/Main Camera").transform;
		}

		private bool shouldTeleport { get; set; }
		private Vector3 teleportLocation;
		private bool teleportMoveCamera;
		public void Teleport(Vector3 location) { Teleport(location, true); }
		public void Teleport(Vector3 location, bool moveCamera)
        {
			teleportLocation = location;
			teleportMoveCamera = moveCamera;
			shouldTeleport = true;
		}
		private void Teleport()
        {
			Vector3 b = Camera.main.transform.position - this.player.transform.position;
			Vector3 b2 = cursor.position - player.transform.position;
			if (teleportMoveCamera) player.transform.position = teleportLocation;
			camera.position = player.transform.position + b;
			cursor.position = player.transform.position + b2;
		}

		public void LateUpdate()
        {
			if (shouldTeleport)
            {
				Teleport();
				shouldTeleport = false;
            }
        }
	}
}
