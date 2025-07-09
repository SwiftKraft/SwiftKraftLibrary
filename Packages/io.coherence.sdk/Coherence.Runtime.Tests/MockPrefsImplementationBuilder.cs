// Copyright (c) coherence ApS.
// See the license file in the package root for more information.
namespace Coherence.Runtime.Tests
{
    using System;
    using Moq;
    using Prefs;

    /// <summary>
    /// Can be used to build a mock <see cref="IPrefsImplementation"/>
    /// and <see cref="Inject">inject</see> it into the <see cref="Prefs.Implementation"/> property
    /// for purposes of testing.
    /// <para>
    /// Can be used with a using statement to automatically restore <see cref="IPrefsImplementation"/>
    /// to have the value that it had before <see cref="Inject"/> was called.
    /// </para>
    /// </summary>
    public sealed class MockPrefsImplementationBuilder : IDisposable
    {
        private string getStringResult;
        private Action<string, string> setStringCallback;
        private IPrefsImplementation previousImplementation;
        private (string key, string value)? setStringWasCalledWith;
        public bool SetStringWasCalled => setStringWasCalledWith.HasValue;
        public string SetStringWasCalledWithValue => setStringWasCalledWith.Value.value;

        public MockPrefsImplementationBuilder GetStringReturns(string result)
        {
            getStringResult = result;
            return this;
        }

        public MockPrefsImplementationBuilder OnSetStringCalled(Action<string, string> action)
        {
            setStringCallback = action;
            return this;
        }

        /// <summary>
        /// Builds the mock <see cref="IPrefsImplementation"/> object
        /// and injects it into <see cref="Prefs.Implementation"/>.
        /// </summary>
        public MockPrefsImplementationBuilder Inject()
        {
            previousImplementation = Prefs.Implementation;
            Prefs.Implementation = Build();
            return this;
        }

        /// <summary>
        /// Restores <see cref="Prefs.Implementation"/> to have the value that it had
        /// before <see cref="Inject"/> was called.
        /// </summary>
        public void Dispose() => Prefs.Implementation = previousImplementation;

        private IPrefsImplementation Build()
        {
            var mock = new Mock<IPrefsImplementation>();

            if (getStringResult is not null)
            {
                mock.Setup(prefs => prefs.GetString(It.IsAny<string>()))
                    .Returns(getStringResult);
            }

            mock.Setup(prefs => prefs.SetString(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string key, string value) => setStringWasCalledWith = (key, value));

            if (setStringCallback is not null)
            {
                mock.Setup(prefs => prefs.SetString(It.IsAny<string>(), It.IsAny<string>()))
                    .Callback(setStringCallback);
            }

            return mock.Object;
        }
    }
}
