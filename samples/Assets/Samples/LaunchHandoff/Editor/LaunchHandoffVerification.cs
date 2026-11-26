using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameplaySamples.LaunchHandoff.Editor
{
    public static class LaunchHandoffVerification
    {
        private const string ScenePath = "Assets/Samples/LaunchHandoff/Scenes/LaunchHandoff.unity";
        private const float Step = 0.02f;
        private const float ReleaseX = 3.6f;
        private const float ReleaseY = 1.8f;
        private const float GuideVelocityX = 4f;
        private const float GuideVelocityY = 1.1f / 0.9f;
        private const BindingFlags InstanceFields =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static void VerifySample()
        {
            if (!Application.isBatchMode)
                throw new InvalidOperationException("Run this scene verification from Unity batch mode.");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            LaunchHandoffExplorer explorer = scene.GetRootGameObjects()
                .Select(root => root.GetComponent<LaunchHandoffExplorer>())
                .Single(candidate => candidate != null);
            SimulationMode previousMode = Physics.simulationMode;
            Vector3 previousGravity = Physics.gravity;
            float previousStep = Time.fixedDeltaTime;
            try
            {
                Physics.simulationMode = SimulationMode.Script;
                Physics.gravity = new Vector3(0f, -9.81f, 0f);
                Time.fixedDeltaTime = Step;
                // Invoke the same lifecycle methods used by the scene in Play Mode.
                RuntimeMethod("Awake").Invoke(explorer, null);
                RuntimeMethod("RunComparison").Invoke(explorer, null);
                MethodInfo fixedUpdate = RuntimeMethod("FixedUpdate");
                Rigidbody[] bodies = explorer.GetComponentsInChildren<Rigidbody>();
                Rigidbody zero = bodies.Single(body => body.name == "Zero velocity");
                Rigidbody preserve = bodies.Single(body => body.name == "Preserve velocity");

                bool reachedGuide = false;
                for (int iteration = 0; zero.isKinematic && iteration < 100; iteration++)
                {
                    fixedUpdate.Invoke(explorer, null);
                    if (!zero.isKinematic)
                        break;

                    Physics.Simulate(Step);
                    if (iteration == 15)
                    {
                        Near(zero.position.x, preserve.position.x, "same guided x position");
                        Near(zero.position.y, preserve.position.y, "same guided y position");
                        Check(zero.position.x > 0.5f && zero.position.x < 3f, "Both bodies advanced on the guide.");
                        reachedGuide = true;
                    }
                }

                Check(reachedGuide && !zero.isKinematic && !preserve.isKinematic, "Both guide and release states ran.");
                Near(zero.position.x, ReleaseX, "zero transfer x");
                Near(zero.position.y, ReleaseY, "zero transfer y");
                Near(preserve.position.x, ReleaseX, "preserved transfer x");
                Near(preserve.position.y, ReleaseY, "preserved transfer y");
                Near(zero.linearVelocity.magnitude, 0f, "zero transfer velocity");
                Near(preserve.linearVelocity.x, GuideVelocityX, "preserved horizontal transfer velocity");
                Near(preserve.linearVelocity.y, GuideVelocityY, "preserved vertical transfer velocity");

                for (int iteration = 0; !zero.isKinematic && iteration < 100; iteration++)
                {
                    Physics.Simulate(Step);
                    fixedUpdate.Invoke(explorer, null);
                }
                Check(zero.isKinematic && preserve.isKinematic, "Both bodies froze after the observation.");

                var zeroTrace = ReadFinal(explorer, zero);
                var preserveTrace = ReadFinal(explorer, preserve);
                float zeroTravel = zeroTrace.Position.x - ReleaseX;
                float preserveTravel = preserveTrace.Position.x - ReleaseX;
                Near(zeroTrace.Time, 0.60f, "observation duration");
                Near(preserveTrace.Time, zeroTrace.Time, "shared observation time");
                Near(zeroTravel, 0f, "zero horizontal travel");
                Near(preserveTravel, 2.4f, "preserved horizontal travel");
                Near(zeroTrace.Velocity.y, Physics.gravity.y * zeroTrace.Time, "gravity on zero transfer");
                Near(preserveTrace.Velocity.y, GuideVelocityY + Physics.gravity.y * preserveTrace.Time,
                    "gravity on preserved transfer");
                Check(zeroTrace.Count == preserveTrace.Count && zeroTrace.Count > 10,
                    "The trace recorded paired physics samples.");
                Debug.Log(FormattableString.Invariant(
                    $"Launch Handoff Unity PASS: {preserveTrace.Time:F2} s, {preserveTrace.Count} samples, zero {zeroTravel:F2} m, preserve {preserveTravel:F2} m."));
            }
            finally
            {
                Physics.simulationMode = previousMode;
                Physics.gravity = previousGravity;
                Time.fixedDeltaTime = previousStep;

                FieldInfo dotField = typeof(LaunchHandoffExplorer).GetField("dot", InstanceFields);
                if (dotField?.GetValue(explorer) is Texture2D dot)
                {
                    UnityEngine.Object.DestroyImmediate(dot);
                    dotField.SetValue(explorer, null);
                }
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static (Vector3 Position, Vector3 Velocity, float Time, int Count) ReadFinal(
            LaunchHandoffExplorer explorer, Rigidbody body)
        {
            var lanes = ReadField<IEnumerable>(explorer, "lanes");
            object lane = lanes.Cast<object>().Single(candidate => ReadField<Rigidbody>(candidate, "Body") == body);
            var trace = ReadField<IList>(lane, "Trace");
            Check(trace.Count > 0, "The lane recorded at least one sample.");
            object sample = trace[trace.Count - 1];
            return (ReadField<Vector3>(sample, "Position"), ReadField<Vector3>(sample, "Velocity"),
                ReadField<float>(sample, "Time"), trace.Count);
        }

        private static MethodInfo RuntimeMethod(string name) =>
            typeof(LaunchHandoffExplorer).GetMethod(name, InstanceFields) ??
            throw new MissingMethodException(typeof(LaunchHandoffExplorer).FullName, name);

        private static T ReadField<T>(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, InstanceFields) ??
                throw new MissingFieldException(target.GetType().FullName, name);
            return (T)field.GetValue(target);
        }

        private static void Near(float observed, float expected, string check) =>
            Check(Mathf.Abs(observed - expected) <= 0.001f, $"{check}: expected {expected}, observed {observed}.");

        private static void Check(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
