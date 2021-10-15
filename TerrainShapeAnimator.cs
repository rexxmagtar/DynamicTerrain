using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.U2D;

namespace com.pigsels.BubbleTrouble
{
    /// <summary>
    /// Sample class used to test spriteShape dynamic teraformation.
    /// </summary>
    public class TerrainShapeAnimator : MonoBehaviour
    {
        /// <summary>
        /// Animation class. Encapsulates animation logic. Gets edited in unity editor as an item of the <see cref="TerrainShapeAnimator.Animations"/> list.
        /// </summary>
        [Serializable]
        public class PointAnimation
        {
#region Events and delegates

            /// <summary>
            /// Fired when a point animation is started.
            /// </summary>
            public event PointAnimationStarted OnPointAnimationStarted;

            public delegate void PointAnimationStarted(PointAnimation animation);

            /// <summary>
            /// Fired when a terrain shape animation is finished.
            /// </summary>
            public event PointAnimationFinished OnPointAnimationFinished;

            public delegate void PointAnimationFinished(PointAnimation animation, bool isComplete);

#endregion

            //Type of endpoint coordinate space
            public enum PointType
            {
                Absolute,
                RelativeToCurrent,
                RelativeToInitial
            }

            /// <summary>
            /// Specified controller of the spriteShape
            /// </summary>
            public SpriteShapeController spriteShapeController;

            /// <summary>
            /// Show indexes of the spriteShape points.
            /// </summary>
            public bool ShowPointsNumbers = false;

            //Type of endpoint coordinate space
            public PointType Type;

            /// <summary>
            /// Point to interpolate specified spriteShape point to.
            /// </summary>
            public Vector2 EndPoint;

            /// <summary>
            /// Index of spriteShape point to interpolate.
            /// </summary>
            public int PointToMoveNumber;

            /// <summary>
            /// Animation curve determines interpolation behaviour. y value must in range [0,1].
            /// </summary>
            public AnimationCurve InterpolationAnimationCurve;

            /// <summary>
            /// Determines animation speed. Is multiplied by DeltaTime during interpolation.
            /// </summary>
            [Range(.01f, 5f)]
            public float TimeStep = 1;

            /// <summary>
            /// Coroutine which does the animation.
            /// </summary>
            private Coroutine AnimationCoroutine;

            /// <summary>
            /// Determines if animation is running.
            /// </summary>
            public bool IsInterpolatingNow { get; private set; }

            /// <summary>
            /// Identifier of the animation. Must be unique
            /// </summary>
            [FormerlySerializedAs("AnimationId")]
            public string name;

            public Vector2 InitialPosition;

            /// <summary>
            /// Starts the animation. (Starts Coroutine)
            /// </summary>
            public void StartAnimation()
            {
                if (AnimationCoroutine != null || IsInterpolatingNow)
                {
                    Debug.LogWarning("There was an attempt to start animation which is already running.");
                    return;
                }

                OnPointAnimationStarted?.Invoke(this);
                AnimationCoroutine = GameManager.StartSceneCoroutine(Interpolate());
            }

            /// <summary>
            /// Stops the animation. (Stops Coroutine)
            /// </summary>
            public void StopAnimation()
            {
                if (AnimationCoroutine == null || !IsInterpolatingNow)
                {
                    Debug.LogWarning("There was an attempt to stop animation which isn't running.");
                    return;
                }

                GameManager.StopSceneCoroutine(AnimationCoroutine);
                AnimationCoroutine = null;
                IsInterpolatingNow = false;

                OnPointAnimationFinished?.Invoke(this, false);
            }

            /// <summary>
            /// Starts the point interpolation process.
            /// </summary>
            /// <returns></returns>
            private IEnumerator Interpolate()
            {
                if (spriteShapeController == null || spriteShapeController.spline.GetPointCount() == 0)
                {
                    AnimationCoroutine = null;
                    OnPointAnimationFinished?.Invoke(this, false);
                    yield break;
                }

                IsInterpolatingNow = true;

                var pointCurrentPosition = GetControlPointWorldPosition(PointToMoveNumber);

                Vector2 endPoint = GetAbsoluteEndPointPosition();

                var directionVector = ((Vector3)endPoint - pointCurrentPosition);
                var distance = directionVector.magnitude;

                if (distance < float.Epsilon)
                {
                    IsInterpolatingNow = false;
                    AnimationCoroutine = null;
                    OnPointAnimationFinished?.Invoke(this, false);
                    yield break;
                }

                directionVector.Normalize();

                float time = 0;
                float newDisatnceValue = 0;

                do
                {
                    newDisatnceValue = InterpolationAnimationCurve.Evaluate(time);
                    var newPosition = pointCurrentPosition + directionVector * newDisatnceValue * distance;

                    spriteShapeController.spline.SetPosition(PointToMoveNumber, spriteShapeController.gameObject.transform.InverseTransformPoint(newPosition));

                    time += TimeStep * Time.deltaTime;

                    yield return null;

                } while (newDisatnceValue < 1);

                AnimationCoroutine = null;
                IsInterpolatingNow = false;

                OnPointAnimationFinished?.Invoke(this, true);
            }

            /// <summary>
            /// Wrapper for getting spriteShape central points in WorldSpace (by default spriteShape returns LocalSpace coordinates)
            /// </summary>
            /// <param name="index"></param>
            /// <returns></returns>
            public Vector3 GetControlPointWorldPosition(int index)
            {
                return spriteShapeController.gameObject.transform.TransformPoint(spriteShapeController.spline.GetPosition(index));
            }

            /// <summary>
            ///Gets absolute position of the end point regarding its type. 
            /// </summary>
            /// <returns></returns>
            public Vector2 GetAbsoluteEndPointPosition()
            {
                var pointCurrentPosition = GetControlPointWorldPosition(PointToMoveNumber);

                Vector2 endPoint;

                switch (Type)
                {
                    case PointType.Absolute:
                        endPoint = EndPoint;
                        break;
                    case PointType.RelativeToCurrent:
                        endPoint = pointCurrentPosition + (Vector3)EndPoint;
                        break;
                    case PointType.RelativeToInitial:

                        //This method is called in edit time to draw end point absolute position. But there would be no sense to work with initial position in
                        // Edit time cause there is no logical "start" in this case cause the game hasn't started yet. So initial position must be always equal to current point position 
                        if (!Application.isPlaying)
                        {
                            endPoint = pointCurrentPosition + (Vector3)EndPoint;
                        }
                        else
                        {
                            endPoint = InitialPosition + EndPoint;
                        }

                        break;

                    default:
                        endPoint = EndPoint;
                        break;
                }

                return endPoint;
            }
        }


#region Events and delegates

        /// <summary>
        /// Fired when a terrain shape animation is started.
        /// </summary>
        public event TerrainShapeAnimationStarted OnTerrainShapeAnimationStarted;

        public delegate void TerrainShapeAnimationStarted(TerrainShapeAnimator terrainShapeAnimator, PointAnimation animation);

        /// <summary>
        /// Fired when a terrain shape animation is finished.
        /// </summary>
        public event TerrainShapeAnimationFinished OnTerrainShapeAnimationFinished;

        public delegate void TerrainShapeAnimationFinished(TerrainShapeAnimator terrainShapeAnimator, PointAnimation animation, bool isAborted);

#endregion


        /// <summary>
        /// List of the all animations that can be used in play time.
        /// </summary>
        [ReorderableList]
        public List<PointAnimation> Animations = new List<PointAnimation>();

        /// <summary>
        /// Starts a specified animation.
        /// </summary>
        /// <param name="animationId">Identifier of the animation. The animation must belong to <see cref="Animations" list/></param>
        public void StartPointAnimation(string animationId)
        {
            //Debug.Log($"Starting animation: {animationId}");

            var point = Animations.Find(p => p.name == animationId);

            if (point == null)
            {
                Debug.LogError($"The shape animation '{animationId}' does not exist. Please check animation id.");
                return;
            }

            if (point.spriteShapeController == null)
            {
                Debug.LogError($"The shape animation '{animationId}' nas no SpriteShapeController assigned. Please check animation settings.");
                return;
            }

            var currentRunningAnimationsWithSamePoint = Animations.FindAll
                (p => p.PointToMoveNumber == point.PointToMoveNumber && p.IsInterpolatingNow);

            foreach (var pointAnimation in currentRunningAnimationsWithSamePoint)
            {
                Debug.LogWarning($"The shape animation '{animationId}' start has terminated unfinished animation '{pointAnimation.name}' that affected the same shape point.");
                pointAnimation.StopAnimation();
            }

            point.OnPointAnimationStarted += OnPointAnimationStartedHandler;
            point.OnPointAnimationFinished += OnPointAnimationFinishedHandler;

#if UNITY_EDITOR
            if (point.spriteShapeController.spline.GetPointCount() > 9)
            {
                Debug.LogWarning($"Starting animation '{animationId}' for spriteshape that contains more than 9 control points. This may cause huge FPS drop during animation.\n" +
                                 $"It's strongly recommended to create separate SpriteShapeController for this animation containing bare minimum of control points.");
            }
#endif

            point.StartAnimation();
        }

        private void OnPointAnimationStartedHandler(PointAnimation animation)
        {
            OnTerrainShapeAnimationStarted?.Invoke(this, animation);
        }

        private void OnPointAnimationFinishedHandler(PointAnimation animation, bool isComplete)
        {
            animation.OnPointAnimationStarted -= OnPointAnimationStartedHandler;
            animation.OnPointAnimationFinished -= OnPointAnimationFinishedHandler;

            OnTerrainShapeAnimationFinished?.Invoke(this, animation, isComplete);
        }

        /// <summary>
        /// Start list of the animations. Also check for animations of the same point and ignores them in that case.
        /// </summary>
        /// <param name="animationsIds"></param>
        public void StartAnimations(List<String> animationsIds)
        {
            var animationsToStart =
                Animations.FindAll(p => animationsIds.Exists(animId => animId == p.name));

            if (animationsIds.Count < 1)
            {
                Debug.LogWarning($"The shape animation list is empty.");
            }
            else
            {
                foreach (var animationId in animationsIds)
                {
                    StartPointAnimation(animationId);
                }
            }
        }

        public void Start()
        {
            foreach (var pointAnimation in Animations)
            {
                pointAnimation.InitialPosition = pointAnimation.GetControlPointWorldPosition(pointAnimation.PointToMoveNumber);
            }
        }

#if UNITY_EDITOR

#region Animation testing

        /// <summary>
        /// Help method for dropdown property in inspector.
        /// </summary>
        /// <returns></returns>
        private List<string> GetAnimationsIds()
        {
            var result = new List<string>();

            for (int i = 0; i < Animations.Count; i++)
            {
                result.Add(Animations[i].name);
            }

            return result;
        }

        private bool HasAnimations()
        {
            return Animations.Count > 0;
        }

        /// <summary>
        /// Id of the animation that can be tested in play time, after pressing "Play animation" button.
        /// </summary>
        [HorizontalLine, Space, Dropdown("GetAnimationsIds"), NaughtyAttributes.ShowIf("HasAnimations")]
        public string selectedAnimation = "";

        /// <summary>
        /// Starts the animation got using <see cref="selectedAnimation"/>
        /// </summary>
        [Button("Run animation", EButtonEnableMode.Playmode), NaughtyAttributes.ShowIf("HasAnimations")]
        public void InterpolatePoint()
        {
            StartPointAnimation(selectedAnimation);
        }

        /// <summary>
        /// Resets EndPoint to the value of the animated point.
        /// </summary>
        [Button("Reset EndPoint to initial animated point", EButtonEnableMode.Editor), NaughtyAttributes.ShowIf("HasAnimations")]
        private void ResetEndPoint()
        {
            var point = Animations.Find(p => p.name == selectedAnimation);
            if (point == null)
            {
                Debug.LogWarning($"Could not find animation '{selectedAnimation}'.");
                return;
            }
            var ssc = point.spriteShapeController;
            if (ssc == null)
            {
                Debug.LogWarning($"SpriteShapeController is not selected in animation '{selectedAnimation}'.");
                return;
            }

            Undo.RecordObject(this, "point reset");

            if (point.Type == PointAnimation.PointType.Absolute)
            {

                point.EndPoint = ssc.transform.TransformPoint(ssc.spline.GetPosition(point.PointToMoveNumber));
            }
            else
            {
                point.EndPoint = Vector3.zero;
            }
        }

#endregion Animation testing


        private void OnDrawGizmosSelected()
        {
            foreach (var animation in Animations)
            {
                if (animation.spriteShapeController == null || animation.spriteShapeController.spline.GetPointCount() == 0)
                    return;

                if (animation.ShowPointsNumbers)
                {
                    var points = new List<Vector3>();

                    for (int i = 0; i < animation.spriteShapeController.spline.GetPointCount(); i++)
                    {
                        points.Add(animation.GetControlPointWorldPosition(i));
                    }

                    for (int i = 0; i < points.Count; i++)
                    {
                        Gizmos.DrawSphere(points[i], 0.5f);
                        Handles.Label(points[i] + Vector3.down, i.ToString());
                    }

                    var prevColor = Gizmos.color;

                    Gizmos.color = Color.black;

                    Vector2 endPoint = animation.GetAbsoluteEndPointPosition();

                    Gizmos.DrawSphere(endPoint, 0.5f);
                    Handles.Label(endPoint + Vector2.up * 2, $"[{animation.name}]");

                    Gizmos.color = prevColor;
                }

            }
        }
#endif
    }
}