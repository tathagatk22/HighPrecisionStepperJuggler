﻿using System.Collections.Generic;
using HighPrecisionStepperJuggler.MachineLearning;
using UnityEngine;
using c = HighPrecisionStepperJuggler.Constants;

namespace HighPrecisionStepperJuggler
{
    public class ImageProcessingInstructionSender : MonoBehaviour
    {
        [SerializeField] private UVCCameraPlugin _cameraPlugin;
        [SerializeField] private MachineController _machineController;
        [SerializeField] private BallPositionVisualizer _ballPositionVisualizer;
        [SerializeField] private BallDataDebugView _ballDataDebugView;
        [SerializeField] private PredictedPositionVisualizer _predictedPositionVisualizer;
        [SerializeField] private GradientDescentView _gradientDescentViewX;
        [SerializeField] private GradientDescentView _gradientDescentViewY;
        [SerializeField] private GradientDescentView _gradientDescentViewZ;
        [SerializeField] private TargetVisualizer _targetVisualizer;

        private readonly GradientDescent _gradientDescentX = new GradientDescent(
            Constants.NumberOfTrainingSetsUsedForXYGD,
            Constants.NumberOfGDUpdateCyclesXY,
            Constants.AlphaXY
            );
        private readonly GradientDescent _gradientDescentY = new GradientDescent(
            Constants.NumberOfTrainingSetsUsedForXYGD,
            Constants.NumberOfGDUpdateCyclesXY,
            Constants.AlphaXY
            );
        
        private readonly GradientDescent _gradientDescentZ = new GradientDescent(
            Constants.NumberOfTrainingSetsUsedForHeightGD,
            Constants.NumberOfGDUpdateCyclesHeight,
            Constants.AlphaHeight
            );

        private BallData _ballData;
        private int _currentStrategyIndex;
        [SerializeField] private bool _executeControlStrategies;

        private List<IBallControlStrategy> _strategies = new List<IBallControlStrategy>();

        private void Awake()
        {
            _gradientDescentViewX.GradientDescent = _gradientDescentX;
            _gradientDescentViewY.GradientDescent = _gradientDescentY;
            _gradientDescentViewZ.GradientDescent = _gradientDescentZ;

            AnalyticalTiltController.Instance.TargetVisualizer = _targetVisualizer;
            PIDTiltController.Instance.TargetVisualizer = _targetVisualizer;
        }

        private void Start()
        {
            _ballData = new BallData(
                _ballDataDebugView,
                _predictedPositionVisualizer,
                _gradientDescentX,
                _gradientDescentY,
                _gradientDescentZ);

            _strategies.Add(BallControlStrategyFactory.GoTo(0.01f));
            _strategies.Add(BallControlStrategyFactory.GoTo(0.05f));

            GetBallBouncing();
            
            _strategies.Add(BallControlStrategyFactory.Continuous2StepBouncing(20, AnalyticalTiltController.Instance));
            _strategies.Add(BallControlStrategyFactory.Continuous2StepBouncing(20, AnalyticalTiltController.Instance, new Vector2(40f, 0f)));
            _strategies.Add(BallControlStrategyFactory.Continuous2StepBouncing(20, AnalyticalTiltController.Instance, new Vector2(0f, 0f)));
            _strategies.Add(BallControlStrategyFactory.Continuous2StepBouncing(20, AnalyticalTiltController.Instance, new Vector2(-40f, 0f)));
            _strategies.Add(BallControlStrategyFactory.Continuous2StepBouncing(20, AnalyticalTiltController.Instance, new Vector2(0f, 0f)));
            _strategies.Add(BallControlStrategyFactory.Continuous2StepBouncing(20, AnalyticalTiltController.Instance, new Vector2(40f, 0f)));
            _strategies.Add(BallControlStrategyFactory.Continuous2StepBouncing(20, AnalyticalTiltController.Instance, new Vector2(0f, 0f)));
            _strategies.Add(BallControlStrategyFactory.Continuous2StepBouncing(20, AnalyticalTiltController.Instance, new Vector2(-40f, 0f)));
            
            for (int i = 0; i < 5; i++)
            {
                _strategies.Add(
                    BallControlStrategyFactory.ContinuousBouncing(5, AnalyticalTiltController.Instance));
                _strategies.Add(
                    BallControlStrategyFactory.ContinuousBouncingStrong(1, AnalyticalTiltController.Instance));
                _strategies.Add(BallControlStrategyFactory.Balancing(0.05f, 8, Vector2.zero,
                    AnalyticalTiltController.Instance));
            }

            _strategies.Add(BallControlStrategyFactory.GoTo(0.01f));
            _strategies.Add(BallControlStrategyFactory.GoTo(0.08f));
            _strategies.Add(BallControlStrategyFactory.GoTo(0.05f));

            GetBallBouncing();

            _strategies.Add(BallControlStrategyFactory.ContinuousBouncing(20, AnalyticalTiltController.Instance));

            for (int i = 0; i < 5; i++)
            {
                _strategies.Add(
                    BallControlStrategyFactory.ContinuousBouncing(5, AnalyticalTiltController.Instance));
                _strategies.Add(
                    BallControlStrategyFactory.ContinuousBouncingStrong(1, AnalyticalTiltController.Instance));
                _strategies.Add(
                    BallControlStrategyFactory.Balancing(0.05f, 8, Vector2.zero, AnalyticalTiltController.Instance));
            }

            _strategies.Add(BallControlStrategyFactory.GoTo(0.01f));
        }

        private void GetBallBouncing()
        {
            for (int i = 0; i < 5; i++)
            {
                _strategies.Add(BallControlStrategyFactory.ContinuousBouncing(5, PIDTiltController.Instance));
                _strategies.Add(BallControlStrategyFactory.ContinuousBouncingStrong(1, PIDTiltController.Instance));
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _executeControlStrategies = !_executeControlStrategies;
            }

            var ballRadiusAndPosition = _cameraPlugin.UpdateImageProcessing();
            var height = FOVCalculations.RadiusToDistance(ballRadiusAndPosition.Radius);

            if (!_executeControlStrategies)
            {
                foreach (var strategy in _strategies)
                {
                    strategy.Reset();
                }

                _currentStrategyIndex = 0;
            }

            if (height >= float.MaxValue)
            {
                // couldn't find ball in image
                return;
            }
            
            var ballPosX = FOVCalculations.PixelPositionToDistanceFromCenter(ballRadiusAndPosition.PositionX, height);
            _gradientDescentX.Hypothesis.SetTheta_0To(ballPosX);
            _gradientDescentX.AddTrainingSet(new TrainingSet(0f, ballPosX));
            _gradientDescentX.UpdateHypothesis();
            
            var ballPosY = FOVCalculations.PixelPositionToDistanceFromCenter(ballRadiusAndPosition.PositionY, height);
            _gradientDescentY.Hypothesis.SetTheta_0To(ballPosY);
            _gradientDescentY.AddTrainingSet(new TrainingSet(0f, ballPosY));
            _gradientDescentY.UpdateHypothesis();
            
            _gradientDescentZ.Hypothesis.SetTheta_0To(height);
            _gradientDescentZ.AddTrainingSet(new TrainingSet(0f, height));
            _gradientDescentZ.UpdateHypothesis();

            _ballData.UpdateData(
                new Vector3(
                    _gradientDescentX.Hypothesis.Parameters.Theta_0,
                    _gradientDescentY.Hypothesis.Parameters.Theta_0,
                    height), 
                new Vector3(
                    _gradientDescentX.Hypothesis.Parameters.Theta_1,
                    _gradientDescentY.Hypothesis.Parameters.Theta_1,
                    _gradientDescentZ.Hypothesis.Parameters.Theta_1
                    ));

            _ballPositionVisualizer.SpawnPositionPoint(_ballData.CurrentUnityPositionVector);

            if (_machineController.IsReadyForNextInstruction && _executeControlStrategies)
            {
                var isRequestingNextStrategy =
                    _strategies[_currentStrategyIndex].Execute(_ballData, _machineController);

                if (isRequestingNextStrategy)
                {
                    _strategies[_currentStrategyIndex].Reset();

                    if (_currentStrategyIndex < _strategies.Count - 1)
                    {
                        _currentStrategyIndex++;
                        
                        _predictedPositionVisualizer
                            .SetActive(_strategies[_currentStrategyIndex].UsesBallPositionPrediction);
                    }
                }
            }
        }
    }
}
