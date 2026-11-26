using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameplaySamples.EvmLite.Editor
{
    public static class EvmLiteVerification
    {
        private const string DeadlineKey = "GameplaySamples.EvmLite.LifecycleDeadline";
        private const float TimeoutSeconds = 45f;

        public static void VerifyLifecycle()
        {
            if (!Application.isBatchMode)
            {
                Debug.LogWarning("Run the EVM Lite lifecycle verifier with -batchmode.");
                return;
            }
            if (Array.Exists(Environment.GetCommandLineArgs(), arg => string.Equals(arg, "-quit", StringComparison.OrdinalIgnoreCase)))
            {
                Finish(1, "Start without -quit so the verifier can enter Play Mode and report its exit code.");
                return;
            }

            SessionState.SetFloat(DeadlineKey, (float)EditorApplication.timeSinceStartup + TimeoutSeconds);
            AttachObservers();
            try
            {
                EditorApplication.EnterPlaymode();
            }
            catch (Exception error)
            {
                Finish(1, error.ToString());
            }
        }

        [InitializeOnLoadMethod]
        private static void AttachObservers()
        {
            // Reattach after Unity reloads the managed domain on entry to Play Mode.
            if (!Application.isBatchMode || SessionState.GetFloat(DeadlineKey, 0f) <= 0f)
                return;

            EditorApplication.playModeStateChanged -= VerifyOnEntry;
            EditorApplication.playModeStateChanged += VerifyOnEntry;
            EditorApplication.update -= CheckTimeout;
            EditorApplication.update += CheckTimeout;
        }

        private static void CheckTimeout()
        {
            float deadline = SessionState.GetFloat(DeadlineKey, 0f);
            if (deadline > 0f && EditorApplication.timeSinceStartup >= deadline)
                Finish(1, $"EVM Lite lifecycle verification timed out after {TimeoutSeconds:0} seconds.");
        }

        private static void VerifyOnEntry(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode)
                return;

            EditorApplication.playModeStateChanged -= VerifyOnEntry;
            try
            {
                var bus = new EventBus();
                int received = 0;
                Action<RoomSignal> onSignal = _ => received++;

                Scene room = SceneManager.CreateScene("EVM Lite Lifecycle Check");
                var host = new GameObject("Receiver");
                host.SetActive(false);
                SceneManager.MoveGameObjectToScene(host, room);
                var receiver = host.AddComponent<RoomSignalReceiver>();
                MethodInfo initialize = typeof(RoomSignalReceiver).GetMethod("Initialize", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new MissingMethodException(typeof(RoomSignalReceiver).FullName, "Initialize");
                void Configure() => initialize.Invoke(receiver, new object[] { bus, onSignal });

                void ExpectCount(int expected, string step)
                {
                    bus.Publish(new RoomSignal(1));
                    if (received != expected)
                        throw new InvalidOperationException($"{step}: expected {expected} total deliveries, saw {received}.");
                }

                Configure();
                ExpectCount(0, "Initial inactive receiver");
                host.SetActive(true);
                ExpectCount(1, "Activated root");
                Configure();
                ExpectCount(2, "Same configuration repeated");
                receiver.enabled = false;
                ExpectCount(2, "Disabled receiver component");
                receiver.enabled = true;
                ExpectCount(3, "Reenabled receiver component");
                host.SetActive(false);
                ExpectCount(3, "Deactivated root");
                host.SetActive(true);
                ExpectCount(4, "Reactivated root");

                AsyncOperation unload = SceneManager.UnloadSceneAsync(room);
                if (unload == null)
                    throw new InvalidOperationException("Unity could not unload the temporary room.");

                unload.completed += _ =>
                {
                    try
                    {
                        ExpectCount(4, "Additive scene unloaded");
                        Finish(0, "EVM Lite Unity PASS: receiver activation, repeat configuration, disable, reenable and additive unload.");
                    }
                    catch (Exception error)
                    {
                        Finish(1, error.ToString());
                    }
                };
            }
            catch (Exception error)
            {
                Finish(1, error.ToString());
            }
        }

        private static void Finish(int exitCode, string message)
        {
            if (!Application.isBatchMode)
                return;

            EditorApplication.playModeStateChanged -= VerifyOnEntry;
            EditorApplication.update -= CheckTimeout;
            SessionState.EraseFloat(DeadlineKey);
            if (exitCode == 0)
                Debug.Log(message);
            else
                Debug.LogError(message);
            EditorApplication.Exit(exitCode);
        }
    }
}
