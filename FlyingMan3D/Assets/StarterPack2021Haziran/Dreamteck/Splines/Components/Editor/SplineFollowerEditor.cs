namespace Dreamteck.Splines.Editor
{
    using UnityEngine;
    using System.Collections;
    using UnityEditor;

    [CustomEditor(typeof(SplineFollower), true)]
    [CanEditMultipleObjects]
    public class SplineFollowerEditor : SplineTracerEditor
    {
        SplineSample result = new SplineSample();
        protected SplineFollower[] followers = new SplineFollower[0];
        protected SerializedObject serializedFollowers;
        protected FollowerSpeedModifierEditor speedModifierEditor;

        void OnSetDistance(float distance)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                SplineFollower follower = (SplineFollower)targets[i];
                double travel = follower.Travel(0.0, distance, Spline.Direction.Forward);
                follower.startPosition = travel;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            followers = new SplineFollower[users.Length];
            for (int i = 0; i < followers.Length; i++)
            {
                followers[i] = (SplineFollower)users[i];
            }

            if (followers.Length == 1)
            {
                speedModifierEditor = new FollowerSpeedModifierEditor(followers[0], this, followers[0].speedModifier);
            }
        }

        protected override void BodyGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Following", EditorStyles.boldLabel);
            SplineFollower follower = (SplineFollower)target;

            serializedFollowers = new SerializedObject(followers);
            SerializedProperty followMode = serializedObject.FindProperty("followMode");
            SerializedProperty preserveUniformSpeedWithOffset = serializedObject.FindProperty("preserveUniformSpeedWithOffset");
            SerializedProperty wrapMode = serializedObject.FindProperty("wrapMode");
            SerializedProperty startPosition = serializedObject.FindProperty("_startPosition");
            SerializedProperty autoStartPosition = serializedObject.FindProperty("autoStartPosition");
            SerializedProperty follow = serializedObject.FindProperty("follow");
            SerializedProperty unityOnEndReached = serializedObject.FindProperty("_unityOnEndReached");
            SerializedProperty unityOnBeginningReached = serializedObject.FindProperty("_unityOnBeginningReached");

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(follow);
            if (follow.boolValue)
            {
                EditorGUILayout.PropertyField(followMode);
                if (followMode.intValue == (int)SplineFollower.FollowMode.Uniform)
                {
                    SerializedProperty followSpeed = serializedObject.FindProperty("_followSpeed");
                    SerializedProperty motion = serializedObject.FindProperty("_motion");
                    SerializedProperty motionHasOffset = motion.FindPropertyRelative("_hasOffset");

                    EditorGUILayout.PropertyField(followSpeed, new GUIContent("Follow Speed"));
                    if (followSpeed.floatValue < 0f)
                    {
                        followSpeed.floatValue = 0f;
                    }
                    if (motionHasOffset.boolValue)
                    {
                        EditorGUILayout.PropertyField(preserveUniformSpeedWithOffset, new GUIContent("Preserve Uniform Speed With Offset"));
                    }
                    if (followers.Length == 1)
                    {
                        speedModifierEditor.DrawInspector();
                    }
                }
                else
                {
                    follower.followDuration = EditorGUILayout.FloatField("Follow duration", follower.followDuration);
                }
            }


            EditorGUILayout.PropertyField(wrapMode);


            if (follower.motion.applyRotation)
            {
                follower.applyDirectionRotation = EditorGUILayout.Toggle("Face Direction", follower.applyDirectionRotation);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Start Position", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(autoStartPosition, new GUIContent("Project"));
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 100f;
            if (!follower.autoStartPosition && !Application.isPlaying)
            {
                EditorGUILayout.PropertyField(startPosition, new GUIContent("Start Position"));
                if (GUILayout.Button("Set Distance", GUILayout.Width(85)))
                {
                    DistanceWindow w = EditorWindow.GetWindow<DistanceWindow>(true);
                    w.Init(OnSetDistance, follower.CalculateLength());
                }
            }
            else
            {
                EditorGUILayout.LabelField("Start position", GUILayout.Width(EditorGUIUtility.labelWidth));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(unityOnBeginningReached);
            EditorGUILayout.PropertyField(unityOnEndReached);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                if (!Application.isPlaying)
                {
                    for (int i = 0; i < followers.Length; i++)
                    {
                        if(followers[i].spline.sampleCount > 0)
                        {
                            if (!followers[i].autoStartPosition)
                            {
                                followers[i].SetPercent(startPosition.floatValue);
                                if (!followers[i].follow) SceneView.RepaintAll();
                            }
                        }
                    }
                }
            }

            base.BodyGUI();
        }


        protected override void OnSceneGUI()
        {
            base.OnSceneGUI();
            SplineFollower user = (SplineFollower)target;
            if (user == null) return;
            if (Application.isPlaying)
            {
                if (!user.follow) DrawResult(user.modifiedResult);
                return;
            }
            if (user.spline == null) return;
            if (user.autoStartPosition)
            {
                user.spline.Project(result, user.transform.position, user.clipFrom, user.clipTo);
                DrawResult(result);
            } else if(!user.follow) DrawResult(user.result);

            if (followers.Length == 1)
            {
                speedModifierEditor.DrawScene();
            }

        }
    }
}
