﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Queues;
using Microsoft.Azure.WebJobs.Host.Queues.Triggers;
using Microsoft.Azure.WebJobs.Host.Storage.Queue;
using Microsoft.Azure.WebJobs.Host.TestCommon;
using Microsoft.Azure.WebJobs.Host.Timers;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.WindowsAzure.Storage.Queue;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Azure.WebJobs.Host.UnitTests.Queues
{
    public class QueueTriggerBindingIntegrationTests : IClassFixture<InvariantCultureFixture>
    {
        private ITriggerBinding _binding;

        public QueueTriggerBindingIntegrationTests()
        {
            IQueueTriggerArgumentBindingProvider provider = new UserTypeArgumentBindingProvider();
            ParameterInfo pi = new StubParameterInfo("parameterName", typeof(UserDataType));
            var argumentBinding = provider.TryCreate(pi);
            Mock<IStorageQueue> queueMock = new Mock<IStorageQueue>(MockBehavior.Strict);
            queueMock.Setup(q => q.Name).Returns("queueName");
            IStorageQueue queue = queueMock.Object;
            _binding = new QueueTriggerBinding("parameterName", queue, argumentBinding,
                new Mock<IQueueConfiguration>(MockBehavior.Strict).Object, BackgroundExceptionDispatcher.Instance,
                new Mock<IContextSetter<IMessageEnqueuedWatcher>>(MockBehavior.Strict).Object,
                new SharedContextProvider(), new TestTraceWriter(TraceLevel.Verbose));
        }

        [Theory]
        [InlineData("RequestId", "4b957741-c22e-471d-9f0f-e1e8534b9cb6")]
        [InlineData("RequestReceivedTime", "8/16/2014 12:09:36 AM")]
        [InlineData("DeliveryCount", "8")]
        [InlineData("IsSuccess", "False")]
        public void BindAsync_IfUserDataType_ReturnsValidBindingData(string userPropertyName, string userPropertyValue)
        {
            // Arrange
            UserDataType expectedObject = new UserDataType();
            PropertyInfo userProperty = typeof(UserDataType).GetProperty(userPropertyName);
            var parseMethod = userProperty.PropertyType.GetMethod(
                "Parse", new Type[] { typeof(string) });
            object convertedPropertyValue = parseMethod.Invoke(null, new object[] { userPropertyValue });
            userProperty.SetValue(expectedObject, convertedPropertyValue);
            string messageContent = JsonConvert.SerializeObject(expectedObject);
            CloudQueueMessage message = new CloudQueueMessage(messageContent);

            // Act
            ITriggerData data = _binding.BindAsync(message, null).GetAwaiter().GetResult();

            // Assert
            Assert.NotNull(data);
            Assert.NotNull(data.ValueProvider);
            Assert.NotNull(data.BindingData);
            Assert.True(data.BindingData.ContainsKey(userPropertyName));
            Assert.Equal(userProperty.GetValue(expectedObject, null), data.BindingData[userPropertyName]);
        }

        private class StubParameterInfo : ParameterInfo
        {
            public StubParameterInfo(string name, Type type)
            {
                NameImpl = name;
                ClassImpl = type;
            }
        }

        public class UserDataType
        {
            public Guid RequestId { get; set; }
            public string BlobFile { get; set; }
            public DateTime RequestReceivedTime { get; set; }
            public Int32 DeliveryCount { get; set; }
            public Boolean IsSuccess { get; set; }
        }

        public class DerivedUserDataType : UserDataType
        {
            public Boolean IsDerived { get; set; }
        }

        [Theory]
        [InlineData("RequestId", "4b957741-c22e-471d-9f0f-e1e8534b9cb6")]
        [InlineData("RequestReceivedTime", "8/16/2014 12:09:36 AM")]
        [InlineData("DeliveryCount", "8")]
        [InlineData("IsSuccess", "False")]
        [InlineData("IsDerived", "True")]
        public void BindAsync_IfDerivedUserDataType_ReturnsValidBindingData(string userPropertyName, string userPropertyValue)
        {
            // Arrange
            DerivedUserDataType expectedObject = new DerivedUserDataType();
            PropertyInfo userProperty = typeof(DerivedUserDataType).GetProperty(userPropertyName);
            var parseMethod = userProperty.PropertyType.GetMethod(
                "Parse", new Type[] { typeof(string) });
            object convertedPropertyValue = parseMethod.Invoke(null, new object[] { userPropertyValue });
            userProperty.SetValue(expectedObject, convertedPropertyValue);
            string messageContent = JsonConvert.SerializeObject(expectedObject, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
            CloudQueueMessage message = new CloudQueueMessage(messageContent);

            // Act
            ITriggerData data = _binding.BindAsync(message, null).GetAwaiter().GetResult();

            // Assert
            Assert.NotNull(data);
            Assert.NotNull(data.ValueProvider);
            Assert.NotNull(data.BindingData);
            Assert.True(data.BindingData.ContainsKey(userPropertyName));
            Assert.Equal(userProperty.GetValue(expectedObject, null), data.BindingData[userPropertyName]);
        }


    }
}
