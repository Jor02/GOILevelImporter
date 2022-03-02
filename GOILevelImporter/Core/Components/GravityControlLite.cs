using System;
using UnityEngine;

namespace GOILevelImporter.Core.Components
{
	public class GravityControlLite : MonoBehaviour
	{
		private void OnTriggerStay2D(Collider2D coll)
		{
			foreach (GravityControlLite.Attractors attractors in gravityWells)
			{
				gvec = (Vector2)attractors.gravityWell.position - coll.attachedRigidbody.position;
				coll.attachedRigidbody.AddForce(2500f / gvec.sqrMagnitude * gvec.normalized * (1f + attractors.gravityModifier));
			}
		}
		
		private void OnTriggerEnter2D(Collider2D coll)
		{
			Physics2D.gravity = new Vector2(0f, 0f);
		}

		private void OnTriggerExit2D(Collider2D coll)
		{
			if (coll.attachedRigidbody != null)
			{
				Physics2D.gravity = new Vector2(0f, -30f);
			}
		}

		private Vector2 gvec;

		public GravityControlLite.Attractors[] gravityWells;

		[Serializable]
		public class Attractors
		{
			public Transform gravityWell;
			public float gravityModifier;
		}
	}
}