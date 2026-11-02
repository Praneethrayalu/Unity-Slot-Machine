using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.Editor
{
    public static partial class SlotProjectBuilder
    {
        /// <summary>Animator controllers created for the scene.</summary>
        internal sealed class AnimationSet
        {
            public AnimatorController Lever;
            public AnimatorController Popup;
            public AnimatorController SpinButton;
            public AnimatorController MachineLights;
            public AnimatorController BannerPulse;
        }

        private static AnimationSet CreateAnimations()
        {
            EnsureFolder(AnimationFolder);
            return new AnimationSet
            {
                Lever = CreateLeverController(),
                Popup = CreatePopupController(),
                SpinButton = CreateSpinButtonController(),
                MachineLights = CreateMachineLightsController(),
                BannerPulse = CreateBannerPulseController(),
            };
        }

        // Lever: sprite swap up -> down -> up, played on every spin.
        private static AnimatorController CreateLeverController()
        {
            Sprite up = LoadSprite($"{MachineArtFolder}/slot-machine2.png");
            Sprite down = LoadSprite($"{MachineArtFolder}/slot-machine3.png");

            AnimationClip idle = SaveClip(new AnimationClip { name = "Lever_Idle" }, false, clip =>
                SetSpriteKeys(clip, string.Empty, (0f, up)));

            AnimationClip pull = SaveClip(new AnimationClip { name = "Lever_Pull" }, false, clip =>
            {
                SetSpriteKeys(clip, string.Empty, (0f, up), (0.06f, down), (0.34f, up), (0.4f, up));
            });

            AnimatorController controller = NewController("Lever");
            controller.AddParameter("Pull", AnimatorControllerParameterType.Trigger);
            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState idleState = AddState(sm, "Idle", idle);
            AnimatorState pullState = AddState(sm, "Pull", pull);
            sm.defaultState = idleState;

            AnimatorStateTransition toPull = sm.AddAnyStateTransition(pullState);
            toPull.AddCondition(AnimatorConditionMode.If, 0f, "Pull");
            toPull.duration = 0f;
            toPull.canTransitionToSelf = true;

            ExitAfterClip(pullState, idleState);
            return controller;
        }

        // Shared by every popup: the child named "Panel" pops in with an overshoot and shrinks out.
        private static AnimatorController CreatePopupController()
        {
            AnimationClip show = SaveClip(new AnimationClip { name = "Popup_Show" }, false, clip =>
            {
                AnimationCurve scale = Curve((0f, 0.55f), (0.16f, 1.08f), (0.26f, 0.97f), (0.34f, 1f));
                SetScaleCurve(clip, "Panel", scale);
            });

            AnimationClip hide = SaveClip(new AnimationClip { name = "Popup_Hide" }, false, clip =>
                SetScaleCurve(clip, "Panel", Curve((0f, 1f), (0.16f, 0.8f))));

            AnimatorController controller = NewController("Popup");
            controller.AddParameter("Show", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hide", AnimatorControllerParameterType.Trigger);
            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState hidden = AddState(sm, "Hidden", null);
            AnimatorState shown = AddState(sm, "Show", show);
            AnimatorState hiding = AddState(sm, "Hide", hide);
            sm.defaultState = hidden;

            AnimatorStateTransition toShow = sm.AddAnyStateTransition(shown);
            toShow.AddCondition(AnimatorConditionMode.If, 0f, "Show");
            toShow.duration = 0f;
            toShow.canTransitionToSelf = true;

            AnimatorStateTransition toHide = sm.AddAnyStateTransition(hiding);
            toHide.AddCondition(AnimatorConditionMode.If, 0f, "Hide");
            toHide.duration = 0f;
            toHide.canTransitionToSelf = false;

            return controller;
        }

        // Spin button "breathes" while the machine is idle to invite the next spin.
        private static AnimatorController CreateSpinButtonController()
        {
            AnimationClip rest = SaveClip(new AnimationClip { name = "SpinButton_Rest" }, false, clip =>
                SetScaleCurve(clip, string.Empty, Curve((0f, 1f))));

            AnimationClip pulse = SaveClip(new AnimationClip { name = "SpinButton_Pulse" }, true, clip =>
                SetScaleCurve(clip, string.Empty, Curve((0f, 1f), (0.6f, 1.05f), (1.2f, 1f))));

            AnimatorController controller = NewController("SpinButton");
            controller.AddParameter("Ready", AnimatorControllerParameterType.Bool);
            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState restState = AddState(sm, "Rest", rest);
            AnimatorState pulseState = AddState(sm, "Pulse", pulse);
            sm.defaultState = restState;

            AnimatorStateTransition toPulse = restState.AddTransition(pulseState);
            toPulse.AddCondition(AnimatorConditionMode.If, 0f, "Ready");
            toPulse.duration = 0.1f;
            toPulse.hasExitTime = false;

            AnimatorStateTransition toRest = pulseState.AddTransition(restState);
            toRest.AddCondition(AnimatorConditionMode.IfNot, 0f, "Ready");
            toRest.duration = 0.1f;
            toRest.hasExitTime = false;
            return controller;
        }

        // Cabinet lights: the glow behind the JACKPOT sign flashes on wins, harder on jackpots.
        private static AnimatorController CreateMachineLightsController()
        {
            AnimationClip off = SaveClip(new AnimationClip { name = "Lights_Off" }, false, clip =>
            {
                SetAlphaCurve(clip, "TitleGlow", Curve((0f, 0f)));
                SetAlphaCurve(clip, "CabinetGlow", Curve((0f, 0f)));
            });

            AnimationClip celebrate = SaveClip(new AnimationClip { name = "Lights_Celebrate" }, false, clip =>
            {
                SetAlphaCurve(clip, "TitleGlow", Flicker(2.4f, 0.3f, 0.9f));
                SetAlphaCurve(clip, "CabinetGlow", Curve((0f, 0f), (0.3f, 0.35f), (2.1f, 0.35f), (2.4f, 0f)));
            });

            AnimationClip jackpot = SaveClip(new AnimationClip { name = "Lights_Jackpot" }, false, clip =>
            {
                SetAlphaCurve(clip, "TitleGlow", Flicker(4.8f, 0.15f, 1f));
                SetAlphaCurve(clip, "CabinetGlow", Flicker(4.8f, 0.3f, 0.7f));
            });

            AnimatorController controller = NewController("MachineLights");
            controller.AddParameter("Celebrate", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Jackpot", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Stop", AnimatorControllerParameterType.Trigger);
            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState offState = AddState(sm, "Off", off);
            AnimatorState celebrateState = AddState(sm, "Celebrate", celebrate);
            AnimatorState jackpotState = AddState(sm, "Jackpot", jackpot);
            sm.defaultState = offState;

            foreach ((AnimatorState state, string trigger) in new[] { (celebrateState, "Celebrate"), (jackpotState, "Jackpot"), (offState, "Stop") })
            {
                AnimatorStateTransition t = sm.AddAnyStateTransition(state);
                t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
                t.duration = 0f;
                t.canTransitionToSelf = trigger != "Stop";
            }

            ExitAfterClip(celebrateState, offState);
            ExitAfterClip(jackpotState, offState);
            return controller;
        }

        // Gentle looping pulse for the FREE SPINS banner.
        private static AnimatorController CreateBannerPulseController()
        {
            AnimationClip pulse = SaveClip(new AnimationClip { name = "Banner_Pulse" }, true, clip =>
                SetScaleCurve(clip, string.Empty, Curve((0f, 1f), (0.4f, 1.08f), (0.8f, 1f))));

            AnimatorController controller = NewController("BannerPulse");
            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            sm.defaultState = AddState(sm, "Pulse", pulse);
            return controller;
        }

        // ---------------------------------------------------------------- helpers

        private static AnimatorController NewController(string name)
        {
            string path = $"{AnimationFolder}/{name}.controller";
            AssetDatabase.DeleteAsset(path);
            return AnimatorController.CreateAnimatorControllerAtPath(path);
        }

        private static AnimatorState AddState(AnimatorStateMachine sm, string name, Motion motion)
        {
            AnimatorState state = sm.AddState(name);
            state.motion = motion;

            // UI animators share objects with code-driven effects; never snap them back to defaults.
            state.writeDefaultValues = false;
            return state;
        }

        private static void ExitAfterClip(AnimatorState from, AnimatorState to)
        {
            AnimatorStateTransition t = from.AddTransition(to);
            t.hasExitTime = true;
            t.exitTime = 1f;
            t.duration = 0f;
        }

        private static AnimationClip SaveClip(AnimationClip clip, bool loop, System.Action<AnimationClip> build)
        {
            clip.frameRate = 60f;
            build(clip);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return CreateOrReplaceAsset(clip, $"{AnimationFolder}/{clip.name}.anim");
        }

        private static AnimationCurve Curve(params (float time, float value)[] keys)
        {
            var curve = new AnimationCurve();
            foreach ((float time, float value) in keys)
            {
                curve.AddKey(new Keyframe(time, value));
            }

            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            }

            return curve;
        }

        /// <summary>On/off flashing between two alpha levels, ending dark.</summary>
        private static AnimationCurve Flicker(float duration, float period, float high)
        {
            var curve = new AnimationCurve();
            bool on = true;
            for (float t = 0f; t < duration; t += period / 2f)
            {
                curve.AddKey(new Keyframe(t, on ? high : high * 0.25f, 0f, 0f));
                on = !on;
            }

            curve.AddKey(new Keyframe(duration, 0f, 0f, 0f));
            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
            }

            return curve;
        }

        private static void SetScaleCurve(AnimationClip clip, string path, AnimationCurve curve)
        {
            clip.SetCurve(path, typeof(RectTransform), "m_LocalScale.x", curve);
            clip.SetCurve(path, typeof(RectTransform), "m_LocalScale.y", curve);
        }

        private static void SetAlphaCurve(AnimationClip clip, string path, AnimationCurve curve)
        {
            clip.SetCurve(path, typeof(Image), "m_Color.a", curve);
        }

        private static void SetSpriteKeys(AnimationClip clip, string path, params (float time, Sprite sprite)[] keys)
        {
            EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(path, typeof(Image), "m_Sprite");
            var frames = new ObjectReferenceKeyframe[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                frames[i] = new ObjectReferenceKeyframe { time = keys[i].time, value = keys[i].sprite };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);
        }
    }
}
