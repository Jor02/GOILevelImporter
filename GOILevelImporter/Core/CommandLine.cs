using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using GOILevelImporter.Core.Menu;
using GOILevelImporter.Utils;
using UnityEngine;

namespace GOILevelImporter.Core
{
    /// <summary>
    /// Parses the game's launch flags. Every flag is registered below with both
    /// of its names pointing at the method that handles it, so Capture stays a
    /// plain loop and adding a flag means adding one registration.
    /// </summary>
    static class CommandLine
    {
        private sealed class Argument
        {
            public readonly string Long;
            public readonly string Short;
            public readonly bool TakesValue;
            public readonly Action<string> Handler;

            public Argument(string longName, string shortName, bool takesValue, Action<string> handler)
            {
                Long = longName;
                Short = shortName;
                TakesValue = takesValue;
                Handler = handler;
            }

            /// <summary>Name shown in warnings, prefers the long form.</summary>
            public string Label => string.IsNullOrEmpty(Short) ? Long : Long + " / " + Short;
        }

        // Both names of every flag land here, so a lookup doesn't care which
        // form the player used.
        private static readonly Dictionary<string, Argument> Registry =
            new Dictionary<string, Argument>(StringComparer.OrdinalIgnoreCase);

        static CommandLine()
        {
            RegisterValue("--test-level", "-t", HandleTestLevel);
        }

        public static string TestLevelPath { get; private set; }

        public static bool HasTestLevel => !string.IsNullOrEmpty(TestLevelPath);

        /// <summary>
        /// Reads the command line once at startup. Call before anything else needs it.
        /// </summary>
        public static void Capture()
        {
            try
            {
                string[] args = Environment.GetCommandLineArgs();

                for (int i = 0; i < args.Length; i++)
                {
                    string name = args[i];
                    string value = null;

                    // Split "--flag=value" up front so the lookup only sees the name.
                    int equals = name.IndexOf('=');

                    if (equals >= 0)
                    {
                        value = name.Substring(equals + 1);
                        name = name.Substring(0, equals);
                    }

                    Argument argument;

                    if (!Registry.TryGetValue(Normalize(name), out argument))
                    {
                        continue;
                    }

                    if (argument.TakesValue && value == null)
                    {
                        if (i + 1 >= args.Length)
                        {
                            Debug.LogWarning("Flag " + argument.Label + " needs a value.");
                            continue;
                        }

                        i++;
                        value = args[i];
                    }

                    argument.Handler(value);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Could not read command line arguments: " + e.Message);
            }
        }

        /// <summary>
        /// Registers a flag that expects a value.
        /// </summary>
        private static void RegisterValue(string longName, string shortName, Action<string> handler)
        {
            Register(new Argument(longName, shortName, true, handler));
        }

        /// <summary>
        /// Registers an on/off flag that takes no value.
        /// </summary>
        private static void RegisterSwitch(string longName, string shortName, Action handler)
        {
            Register(new Argument(longName, shortName, false, value => handler()));
        }

        /// <summary>
        /// Puts both names of a flag in the registry so either one resolves to
        /// the same handler.
        /// </summary>
        private static void Register(Argument argument)
        {
            Registry[Normalize(argument.Long)] = argument;

            if (!string.IsNullOrEmpty(argument.Short))
            {
                Registry[Normalize(argument.Short)] = argument;
            }
        }

        /// <summary>
        /// Drops the leading dashes so "--test-level", "-test-level" and
        /// "/test-level" all land on the same flag.
        /// </summary>
        private static string Normalize(string name)
        {
            return string.IsNullOrEmpty(name) ? string.Empty : name.TrimStart('-', '/');
        }

        private static void HandleTestLevel(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                Debug.LogWarning("Flag --test-level / -t needs a level path.");
                return;
            }

            value = value.Trim('"');

            if (!File.Exists(value))
            {
                Debug.LogWarning("Test level not found: '" + value + "'.");
                return;
            }

            TestLevelPath = Path.GetFullPath(value);
        }

        /// <summary>
        /// Selects the pending test level and starts the game.
        /// </summary>
        public static IEnumerator AutoStartTestLevel()
        {
            if (!HasTestLevel)
            {
                yield break;
            }

            string path = TestLevelPath;
            TestLevelPath = null;

            LevelFileScanner.Response response;

            try
            {
                response = LevelFileScanner.ReadSingleLevel(path);
            }
            catch (Exception e)
            {
                Debug.LogError("Could not read test level '" + path + "': " + e.Message);
                yield break;
            }

            if (response.Message != LevelFileScanner.Response.ResponseType.Success)
            {
                Debug.LogError("Could not load test level '" + path + "': " + response.Message);
                yield break;
            }

            LevelSelectionState.Select(false, response.LevelPath, response.Legacy, (ulong)response.HeaderSize, response.Metadata);
            LevelSelectionState.ClearPendingScene();

            Loader loader = null;
            float waited = 0f;
            const float timeout = 60f;

            while (loader == null && waited < timeout)
            {
                loader = UnityEngine.Object.FindObjectOfType<Loader>();

                if (loader == null)
                {
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            if (loader == null)
            {
                Debug.LogError("Test level launch failed: no Loader found.");
                yield break;
            }

            while (!loader.GetPrivateFieldValue<bool>("loadFinished") && waited < timeout)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!loader.GetPrivateFieldValue<bool>("loadFinished"))
            {
                Debug.LogError("Test level launch failed: the game never finished loading.");
                yield break;
            }

            Debug.Log("Test level: loading '" + response.LevelPath + "'.");

            LevelLoader.WipeSaves();

            PopulateTransitionScreen(response);
            LevelTransitionScreen.Instance.FadeOut();
            SilenceMenu(loader);
            LevelLoader.Instance.BeginLoadLevel();
            loader.DoStart();
        }

        /// <summary>
        /// Fills the transition overlay from the level we are about to start, so
        /// the loading screen shows the same info the menu would have.
        /// </summary>
        private static void PopulateTransitionScreen(LevelFileScanner.Response response)
        {
            LevelTransitionScreen screen = LevelTransitionScreen.Instance;

            if (screen == null)
            {
                return;
            }

            LevelMetadata metadata = response.Metadata;
            Texture2D thumbnail = metadata.GetThumbnail();

            screen.Name.text = metadata.LevelName;
            screen.Author.text = string.IsNullOrWhiteSpace(metadata.Author) ? "" : "By " + metadata.Author;
            screen.ThumbnailObject.SetActive(thumbnail != null);

            if (thumbnail != null)
            {
                // The scanner decodes to a raw texture while the image wants a sprite.
                screen.Thumbnail.sprite = Sprite.Create(
                    thumbnail,
                    new Rect(0f, 0f, thumbnail.width, thumbnail.height),
                    new Vector2(0.5f, 0.5f));
            }
        }

        /// <summary>
        /// Mutes and hides the main menu so the test launch goes straight to the
        /// loading screen with no menu audio and no button animation.
        /// </summary>
        private static void SilenceMenu(Loader loader)
        {
            loader.StopAllCoroutines();

            foreach (AudioSource source in UnityEngine.Object.FindObjectsOfType<AudioSource>())
            {
                if (source.gameObject.scene == loader.gameObject.scene)
                {
                    source.Stop();
                    source.mute = true;
                }
            }

            loader.SetPrivateFieldValue("shouldShowMouseQuery", false);

            if (loader.mouseQuery != null) loader.mouseQuery.SetActive(false);
            if (loader.humble != null) loader.humble.gameObject.SetActive(false);
            if (loader.fog != null) loader.fog.SetActive(false);
            if (loader.scrapeParticles != null) loader.scrapeParticles.gameObject.SetActive(false);

            loader.menu.gameObject.SetActive(false);
            loader.fader.color = new Color(0f, 0f, 0f, 1f);
        }
    }
}
