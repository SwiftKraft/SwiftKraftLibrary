// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    /// <summary>
    /// Unit tests for <see cref="TaskExtensions"/>.
    /// </summary>
    [TestFixture]
    public class TaskExtensionsTests
    {
        [Test]
        public async Task Then_Executes_Delegate_When_Task_Completes_Successfully()
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            var executed = false;
            taskCompletionSource.Task.Then(() => executed = true);

            taskCompletionSource.SetResult(true);

            // Wait for Unity's synchronization context to execute the continuation
            await Task.Yield();

            Assert.That(executed, Is.True);
        }

        [Test]
        public async Task Then_Executes_Delegate_When_Task_Canceled()
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            var executed = false;
            taskCompletionSource.Task.Then(() => executed = true);

            taskCompletionSource.SetCanceled();

            // Wait for Unity's synchronization context to execute the continuation
            await Task.Yield();

            Assert.That(executed, Is.True);
        }

        [Test]
        public async Task Then_Executes_Delegate_When_Task_Throws()
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            var executed = false;
            taskCompletionSource.Task.Then(() => executed = true);

            taskCompletionSource.SetException(new Exception("Test"));

            // Wait for Unity's synchronization context to execute the continuation
            await Task.Yield();

            Assert.That(executed, Is.True);
        }

        [Test]
        public async Task Then_With_CancellationToken_Executes_Delegate_When_Task_Completes_Successfully()
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            var cancellationTokenSource = new CancellationTokenSource();
            var executed = false;
            taskCompletionSource.Task.Then(() => executed = true, cancellationTokenSource.Token);

            taskCompletionSource.SetResult(true);

            // Wait for Unity's synchronization context to execute the continuation
            await Task.Yield();

            Assert.That(executed, Is.True);
        }

        [Test]
        public async Task Then_With_CancellationToken_Does_Not_Execute_Delegate_When_Task_Completes_Successfully_If_It_Has_Been_Canceled()
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            var cancellationTokenSource = new CancellationTokenSource();
            var executed = false;
            taskCompletionSource.Task.Then(() => executed = true, cancellationTokenSource.Token);
            cancellationTokenSource.Cancel();

            taskCompletionSource.SetResult(true);

            // Wait for Unity's synchronization context to execute the continuation
            await Task.Yield();

            Assert.That(executed, Is.False);
        }

        [TestCase(CompletionType.Success, false)]
        [TestCase(CompletionType.Success, true)]
        [TestCase(CompletionType.Canceled, false)]
        [TestCase(CompletionType.Canceled, true)]
        [TestCase(CompletionType.Faulted, false)]
        [TestCase(CompletionType.Faulted, true)]
        public async Task Then_With_CancellationToken_Does_Not_Execute_Delegate_When_It_Completes_If_It_Has_Been_Canceled(CompletionType completionType, bool cancelUsingToken)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            var cancellationTokenSource = new CancellationTokenSource();
            var executed = false;
            taskCompletionSource.Task.Then(() => executed = true, cancellationTokenSource.Token);

            if (cancelUsingToken)
            {
                cancellationTokenSource.Cancel();
            }

            await CompleteTask(taskCompletionSource, completionType);

            Assert.That(executed, Is.EqualTo(!cancelUsingToken));
        }

        [TestCase(CompletionType.Success, false)]
        [TestCase(CompletionType.Success, true)]
        [TestCase(CompletionType.Canceled, false)]
        [TestCase(CompletionType.Canceled, true)]
        [TestCase(CompletionType.Faulted, false)]
        [TestCase(CompletionType.Faulted, true)]
        public async Task Then_With_ActionTask_And_CancellationToken_Does_Not_Execute_Delegate_When_It_Completes_If_It_Has_Been_Canceled(CompletionType completionType, bool cancelUsingToken)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            var cancellationTokenSource = new CancellationTokenSource();
            var executed = false;
            taskCompletionSource.Task.Then(() => executed = true, cancellationTokenSource.Token);

            if (cancelUsingToken)
            {
                cancellationTokenSource.Cancel();
            }

            await CompleteTask(taskCompletionSource, completionType);

            Assert.That(executed, Is.EqualTo(!cancelUsingToken));
        }

        [TestCase(TaskContinuationOptions.None, CompletionType.Success)]
        [TestCase(TaskContinuationOptions.None, CompletionType.Canceled)]
        [TestCase(TaskContinuationOptions.None, CompletionType.Faulted)]
        [TestCase(TaskContinuationOptions.NotOnCanceled, CompletionType.Success)]
        [TestCase(TaskContinuationOptions.NotOnCanceled, CompletionType.Canceled)]
        [TestCase(TaskContinuationOptions.NotOnCanceled, CompletionType.Faulted)]
        [TestCase(TaskContinuationOptions.NotOnFaulted, CompletionType.Success)]
        [TestCase(TaskContinuationOptions.NotOnFaulted, CompletionType.Canceled)]
        [TestCase(TaskContinuationOptions.NotOnFaulted, CompletionType.Faulted)]
        [TestCase(TaskContinuationOptions.NotOnRanToCompletion, CompletionType.Success)]
        [TestCase(TaskContinuationOptions.NotOnRanToCompletion, CompletionType.Canceled)]
        [TestCase(TaskContinuationOptions.NotOnRanToCompletion, CompletionType.Faulted)]
        public async Task Then_Respects_TaskContinuationOptions(TaskContinuationOptions options, CompletionType completionType)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            var executed = false;
            taskCompletionSource.Task.Then(() => executed = true, options);

            await CompleteTask(taskCompletionSource, completionType);

            var expected = completionType switch
            {
                CompletionType.Success => !options.HasFlag(TaskContinuationOptions.NotOnRanToCompletion),
                CompletionType.Canceled => !options.HasFlag(TaskContinuationOptions.NotOnCanceled),
                CompletionType.Faulted => !options.HasFlag(TaskContinuationOptions.NotOnFaulted),
                _ => throw new ArgumentOutOfRangeException(nameof(completionType), completionType, null)
            };
            Assert.That(executed, Is.EqualTo(expected));
        }

        [TestCase(TaskContinuationOptions.None, CompletionType.Success)]
        [TestCase(TaskContinuationOptions.None, CompletionType.Canceled)]
        [TestCase(TaskContinuationOptions.None, CompletionType.Faulted)]
        [TestCase(TaskContinuationOptions.NotOnCanceled, CompletionType.Success)]
        [TestCase(TaskContinuationOptions.NotOnCanceled, CompletionType.Canceled)]
        [TestCase(TaskContinuationOptions.NotOnCanceled, CompletionType.Faulted)]
        [TestCase(TaskContinuationOptions.NotOnFaulted, CompletionType.Success)]
        [TestCase(TaskContinuationOptions.NotOnFaulted, CompletionType.Canceled)]
        [TestCase(TaskContinuationOptions.NotOnFaulted, CompletionType.Faulted)]
        [TestCase(TaskContinuationOptions.NotOnRanToCompletion, CompletionType.Success)]
        [TestCase(TaskContinuationOptions.NotOnRanToCompletion, CompletionType.Canceled)]
        [TestCase(TaskContinuationOptions.NotOnRanToCompletion, CompletionType.Faulted)]
        public async Task Then_With_ActionTask_Respects_TaskContinuationOptions(TaskContinuationOptions options, CompletionType completionType)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            var executed = false;
            taskCompletionSource.Task.Then(task => executed = true, options);

            await CompleteTask(taskCompletionSource, completionType);

            var expected = completionType switch
            {
                CompletionType.Success => !options.HasFlag(TaskContinuationOptions.NotOnRanToCompletion),
                CompletionType.Canceled => !options.HasFlag(TaskContinuationOptions.NotOnCanceled),
                CompletionType.Faulted => !options.HasFlag(TaskContinuationOptions.NotOnFaulted),
                _ => throw new ArgumentOutOfRangeException(nameof(completionType), completionType, null)
            };
            Assert.That(executed, Is.EqualTo(expected));
        }

        private static async Task CompleteTask(TaskCompletionSource<bool> taskCompletionSource, CompletionType completionType)
        {
            switch (completionType)
            {
                case CompletionType.Success:
                    taskCompletionSource.SetResult(true);
                    break;
                case CompletionType.Canceled:
                    taskCompletionSource.SetCanceled();
                    break;
                case CompletionType.Faulted:
                    taskCompletionSource.SetException(new Exception("Test"));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(completionType), completionType, null);
            }

            // Wait for Unity's synchronization context to execute the continuation
            await Task.Yield();
        }

        public enum CompletionType
        {
            Success,
            Canceled,
            Faulted
        }
    }
}
