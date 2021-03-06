using UnityEngine;
using NUnit.Framework;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Extensions.Sensors;


namespace Unity.MLAgents.Extensions.Tests.Sensors
{

    public static class SensorTestHelper
    {
        public static void CompareObservation(ISensor sensor, float[] expected)
        {
            string errorMessage;
            bool isOK = SensorHelper.CompareObservation(sensor, expected, out errorMessage);
            Assert.IsTrue(isOK, errorMessage);
        }
    }

    public class RigidBodySensorTests
    {
        [Test]
        public void TestNullRootBody()
        {
            var gameObj = new GameObject();

            var sensorComponent = gameObj.AddComponent<RigidBodySensorComponent>();
            var sensor = sensorComponent.CreateSensor();
            SensorTestHelper.CompareObservation(sensor, new float[0]);
        }

        [Test]
        public void TestSingleRigidbody()
        {
            var gameObj = new GameObject();
            var rootRb = gameObj.AddComponent<Rigidbody>();
            var sensorComponent = gameObj.AddComponent<RigidBodySensorComponent>();
            sensorComponent.RootBody = rootRb;
            sensorComponent.Settings = new PhysicsSensorSettings
            {
                UseModelSpaceLinearVelocity = true,
                UseLocalSpaceTranslations = true,
                UseLocalSpaceRotations = true
            };

            var sensor = sensorComponent.CreateSensor();
            sensor.Update();
            var expected = new[]
            {
                0f, 0f, 0f, // ModelSpaceLinearVelocity
                0f, 0f, 0f, // LocalSpaceTranslations
                0f, 0f, 0f, 1f // LocalSpaceRotations
            };
            SensorTestHelper.CompareObservation(sensor, expected);
            Assert.AreEqual(expected.Length, sensorComponent.GetObservationShape()[0]);
        }

        [Test]
        public void TestBodiesWithJoint()
        {
            var rootObj = new GameObject();
            var rootRb = rootObj.AddComponent<Rigidbody>();
            rootRb.velocity = new Vector3(1f, 0f, 0f);

            var middleGamObj = new GameObject();
            var middleRb = middleGamObj.AddComponent<Rigidbody>();
            middleRb.velocity = new Vector3(0f, 1f, 0f);
            middleGamObj.transform.SetParent(rootObj.transform);
            middleGamObj.transform.localPosition = new Vector3(13.37f, 0f, 0f);
            var joint = middleGamObj.AddComponent<ConfigurableJoint>();
            joint.connectedBody = rootRb;

            var leafGameObj = new GameObject();
            var leafRb = leafGameObj.AddComponent<Rigidbody>();
            leafRb.velocity = new Vector3(0f, 0f, 1f);
            leafGameObj.transform.SetParent(middleGamObj.transform);
            leafGameObj.transform.localPosition = new Vector3(4.2f, 0f, 0f);
            var joint2 = leafGameObj.AddComponent<ConfigurableJoint>();
            joint2.connectedBody = middleRb;


            var sensorComponent = rootObj.AddComponent<RigidBodySensorComponent>();
            sensorComponent.RootBody = rootRb;
            sensorComponent.Settings = new PhysicsSensorSettings
            {
                UseModelSpaceTranslations = true,
                UseLocalSpaceTranslations = true,
                UseLocalSpaceLinearVelocity = true
            };

            var sensor = sensorComponent.CreateSensor();
            sensor.Update();
            var expected = new[]
            {
                // Model space
                0f, 0f, 0f, // Root pos
                13.37f, 0f, 0f, // Middle pos
                leafGameObj.transform.position.x, 0f, 0f, // Leaf pos

                // Local space
                0f, 0f, 0f, // Root pos
                0f, 0f, 0f, // Root vel

                13.37f, 0f, 0f, // Attached pos
                -1f, 1f, 0f, // Attached vel

                4.2f, 0f, 0f, // Leaf pos
                0f, -1f, 1f // Leaf vel
            };
            SensorTestHelper.CompareObservation(sensor, expected);
            Assert.AreEqual(expected.Length, sensorComponent.GetObservationShape()[0]);

            // Update the settings to only process joint observations
            sensorComponent.Settings = new PhysicsSensorSettings
            {
                UseJointPositionsAndAngles = true,
                UseJointForces = true,
            };

            sensor = sensorComponent.CreateSensor();
            sensor.Update();

            expected = new[]
            {
                0f, 0f, 0f, // joint1.force
                0f, 0f, 0f, // joint1.torque
                0f, 0f, 0f, // joint2.force
                0f, 0f, 0f, // joint2.torque
            };
            SensorTestHelper.CompareObservation(sensor, expected);
            Assert.AreEqual(expected.Length, sensorComponent.GetObservationShape()[0]);

        }
    }
}
