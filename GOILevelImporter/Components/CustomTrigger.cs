using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using GOILevelImporter.Core;

namespace GOILevelImporter.Components
{
    public class CustomTrigger : MonoBehaviour
    {
        public DetectMode Detect;
        public Trigger TriggerType;
        public bool Mode;
        public Vector3 Destination;
        public AudioClip Sound;
        public Transform[] GravityAtractors;
        public Animation animation;
        public string anim;
        public UnityEvent TriggerEvent;
        public GameObject[] TargetProp;
        public AudioSource source;

        private void Start()
        {
			if (TriggerType == CustomTrigger.Trigger.Playsound)
			{
				source = base.gameObject.GetComponent<AudioSource>();
				if (source == null)
				{
					source = (AudioSource)base.gameObject.AddComponent(typeof(AudioSource));
					return;
				}
			}
		}

		private void OnTriggerEnter2D(Collider2D col)
		{
			//Triggers in the scene being torn down can fire while a level loads.
			if (LevelLoader.Loading) return;

			if ((col.gameObject.layer != 8 && Detect == DetectMode.PlayerOnly) || (col.gameObject.layer == 8 && Detect == DetectMode.PropsOnly))
			{
				return;
			}
			if (Detect == DetectMode.SpecificProp && !TargetProp.Contains(col.gameObject))
			{
				return;
			}
			switch (TriggerType)
			{
				case Trigger.Reset:
					LevelLoader.Instance.Reload(false);
					return;
				case Trigger.Teleport:
					ComponentHelper.Instance.Teleport(Destination);
					if (!Mode)
					{
						Rigidbody2D component = ComponentHelper.Instance.player.GetComponent<Rigidbody2D>();
						if (component != null)
						{
							component.velocity = Vector2.zero;
						}
					}
					return;
				case Trigger.Finish:
					{
						MonoBehaviour.print("Finished");
						PlayerPrefs.SetFloat("LastTime", UnityEngine.Object.FindObjectOfType<Narrator>().timePlayedThisGame);
						PlayerPrefs.DeleteKey("NumSaves");
						PlayerPrefs.DeleteKey("SaveGame0");
						PlayerPrefs.DeleteKey("SaveGame1");
						LevelSelectionState.ClearPendingScene();
						int num = PlayerPrefs.GetInt("NumWins");
						num++;
						PlayerPrefs.SetInt("NumWins", num);
						Debug.Log("Game Done, deleting saves");
						PlayerPrefs.Save();
						SceneManager.LoadScene("Reward Loader");
						return;
					}
				case Trigger.Playsound:
					if (!source.isPlaying)
					{
						source.PlayOneShot(Sound);
					}
					if (Mode)
					{
						Collider2D[] components = base.GetComponents<Collider2D>();
						for (int i = 0; i < components.Length; i++)
						{
							components[i].enabled = false;
						}
					}
					return;
				case Trigger.Animation:
					if (!string.IsNullOrEmpty(anim))
					{
						animation.Play(anim);
						return;
					}
					animation.Play();
					return;
				case Trigger.Event:
					TriggerEvent.Invoke();
					return;
				case Trigger.SwitchScene:
					// Save the sub-scene so the reload below resumes in it, then
					// restart the level through LevelLoader so Async is set and
					// OnSceneLoaded doesn't start a second, parallel load.
					LevelSelectionState.SetPendingScene(anim, LevelLoader.currentBundlePath);

					LevelLoader.Instance.Reload(true);
					return;
				default:
					return;
			}
		}

		//These values are a serialization contract. Old bundles store the enum as an
		//int, so the numbers must not change even if the order does.
		public enum Trigger
        {
            Reset,
            Teleport,
            Finish,
            Playsound,
            Animation = 4,
            Event = 5,
            SwitchScene = 6
        }

        public enum DetectMode
        {
            PlayerOnly,
            PlayerAndProps,
            PropsOnly,
            SpecificProp,
        }
    }
}
