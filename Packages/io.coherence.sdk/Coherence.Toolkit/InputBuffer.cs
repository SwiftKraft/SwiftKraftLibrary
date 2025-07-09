// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Log;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using Logger = Log.Logger;

    /// <summary/>
    /// <param name="input">Stale input that was received.</param>
    /// <param name="frame">Frame of the received input.</param>
    public delegate void StaleInputHandler(object input, long frame);

    public class InputBuffer<T> : IInputBuffer, IInputBufferDebug
        where T : struct
    {
        enum UpdateOperation
        {
            Read,
            Write
        }

        private const int DefaultDelay = 3;

        /// <summary>
        ///     Called when an input is received that is older than the oldest input in the buffer.
        /// </summary>
        public event StaleInputHandler OnStaleInput;

        /// <summary>
        ///     Size of the buffer. Dictates how many frames worth of inputs can be stored. When buffer reaches its capacity a
        ///     pause is requested through the <see cref="ShouldPause" />.
        /// </summary>
        public int Size => inputs.Length;

        /// <summary>
        ///     Last (latest) known input, or `default` if none was added to the buffer yet.
        /// </summary>
        public T LastInput => head < 0 ? default : inputs[head];

        /// <summary>
        ///     Last (latest) frame number for which an input was stored, received or predicted. -1 if nothing was added to the buffer yet.
        /// </summary>
        public long LastFrame => head < 0 ? -1 : frames[head];

        /// <summary>
        ///     Last frame number that was dequeued for sending, or -1 if nothing was sent yet.
        /// </summary>
        public long LastSentFrame { get; private set; } = -1;

        /// <summary>
        ///     Last frame number that was received from the remote input producer, or -1 if nothing was received yet.
        /// </summary>
        public long LastReceivedFrame { get; private set; } = -1;

        /// <summary>
        ///     Last frame received that didn't trigger a misprediction, or -1 if nothing was acknowledged yet. Might be greater
        ///     than <see cref="LastConsumedFrame" />.
        /// </summary>
        public long LastAcknowledgedFrame { get; private set; } = -1;

        /// <summary>
        ///     Last frame that was fetched from the buffer and was not predicted.
        /// </summary>
        public long LastConsumedFrame { get; private set; } = -1;

        /// <summary>
        ///     Frame at which last prediction failure has been detected.
        /// </summary>
        /// <remarks>
        ///     Value is nulled-out as soon as the input is retrieved for that or subsequent frames. For this reason this
        ///     property should be inspected before any input retrieving method in a given frame in order to check whether
        ///     the simulation correction is required.
        /// </remarks>
        public long? MispredictionFrame { get; private set; }

        /// <summary>
        ///     Delay applied to the stored inputs.
        /// </summary>
        public int Delay
        {
            get
            {
                return delay;
            }
            set
            {
                if (value < 0 || value >= Size)
                {
                    throw new ArgumentOutOfRangeException(nameof(Delay), value,
                        $"Invalid delay value. Must be in <0, Size) range.");
                }

                delay = value;
            }
        }

        /// <inheritdoc cref="IInputBuffer.QueueCount"/>
        int IInputBuffer.QueueCount => receiveQueue.Count;

        private readonly T[] inputs;
        private readonly long[] frames;

        private bool hasReceivedNonStaleInput;
        private T lastSentInput;
        private int delay;
        private int baseDelay;
        private int head = -1;
        private readonly bool requiresSubsequentFrames;

        private readonly EqualityComparer<T> inputComparer;
        private readonly Logger logger;

        /// <summary>
        ///     A queue for received inputs that would overwrite not yet processed data if handled immediately.
        /// </summary>
        private readonly SortedList<long, T> receiveQueue;

        public InputBuffer(int bufferSize, int inputDelay = DefaultDelay, bool requiresSubsequentFrames = false, EqualityComparer<T> comparer = null)
        {
            inputs = new T[bufferSize];
            frames = new long[bufferSize];
            delay = inputDelay;
            baseDelay = inputDelay;
            this.requiresSubsequentFrames = requiresSubsequentFrames;
            inputComparer = comparer ?? EqualityComparer<T>.Default;
            receiveQueue = new SortedList<long, T>(bufferSize);

            logger = Log.GetLogger(typeof(InputBuffer<>));
        }

        /// <summary>
        ///     Clears the input buffer and resets its state.
        /// </summary>
        public void Reset()
        {
            for (int i = 0; i < Size; i++)
            {
                inputs[i] = default;
                frames[i] = default;
            }

            receiveQueue.Clear();

            head = -1;
            delay = baseDelay;
            lastSentInput = default;
            LastSentFrame = -1;
            LastReceivedFrame = -1;
            LastConsumedFrame = -1;
            LastAcknowledgedFrame = -1;
            MispredictionFrame = default;
        }

        /// <summary>
        ///     If true, the buffer cannot do any more predictions. Client should pause the game and stop polling inputs to not
        ///     lose any data required for a rollback.
        /// </summary>
        /// <param name="currentFrame">Current simulation frame.</param>
        /// <param name="commonReceivedFrame">Common (lowest) received frame for all inputs in the simulation.</param>
        public bool ShouldPause(long currentFrame, long commonReceivedFrame)
        {
            return (LastReceivedFrame >= 0 && LastReceivedFrame >= commonReceivedFrame &&
                    currentFrame - LastReceivedFrame >= Size - 1)
                   || currentFrame + delay - commonReceivedFrame > Size - 1;
        }

        /// <summary>
        ///     Dequeues next input from the buffer that hasn't been sent yet. Progresses the internal <see cref="LastSentFrame" />
        ///     counter.
        /// </summary>
        /// <returns>True if there is a fresh input to send and the input has changed its value since last sending.</returns>
        public bool DequeueForSending(long currentFrame, out long inputFrame, out T input, out bool differsFromPrevious)
        {
            if (currentFrame > LastFrame)
            {
                Update(currentFrame, UpdateOperation.Read);
            }

            // We're already at the last frame
            if (LastSentFrame >= LastFrame)
            {
                input = default;
                inputFrame = -1;
                differsFromPrevious = false;
                return false;
            }

            int tail = (head + 1) % Size;
            long tailFrame = frames[tail];

            if (LastSentFrame == -1 || LastSentFrame < tailFrame)
            {
                // This is the first input to be sent or buffer was overwritten
                // past last sent frame - start at the beginning of the buffer.
                LastSentFrame = tailFrame - 1;
            }

            LastSentFrame++;

            logger.Trace(nameof(DequeueForSending), (nameof(LastSentFrame), LastSentFrame),
                (nameof(LastFrame), LastFrame), (nameof(tailFrame), tailFrame));

            TryGetInput(LastSentFrame, out input);
            differsFromPrevious = !inputComparer.Equals(lastSentInput, input);
            lastSentInput = input;
            inputFrame = LastSentFrame;
            return true;
        }

        /// <summary>
        ///     Retrieves input from the buffer for a given frame.
        /// </summary>
        /// <returns>False if frame is outside of the buffer range or if no input was added to the buffer yet.</returns>
        public bool TryGetInput(long frame, out T input, bool clearPredictionMark = true)
        {
            DebugPrintBuffer(nameof(TryGetInput));
            logger.Trace(nameof(TryGetInput), (nameof(frame), frame));

            if (frame >= MispredictionFrame && clearPredictionMark)
            {
                MispredictionFrame = null;
            }

            // Take inputs off the queue
            while (receiveQueue.Count > 0)
            {
                T queuedInput = receiveQueue.Values[0];
                long queuedFrame = receiveQueue.Keys[0];

                if (requiresSubsequentFrames)
                {
                    bool isSubsequentInput = queuedFrame == LastReceivedFrame + 1;
                    if (!isSubsequentInput)
                    {
                        break;
                    }
                }

                long maxLastConsumed = Math.Min(frame, queuedFrame);
                if (LastConsumedFrame < maxLastConsumed)
                {
                    LastConsumedFrame = maxLastConsumed;
                }

                bool canDequeueWithoutInputSkip = queuedFrame - LastConsumedFrame < Size;
                if (!canDequeueWithoutInputSkip)
                {
                    FillWithLastInputForNonSubsequent();
                    break;
                }

                bool wouldAffectResult = frame <= LastFrame && frame >= queuedFrame;
                if (wouldAffectResult)
                {
                    // In this case we've just pushed the last consumed frame, so the queue
                    // will move naturally during receive with proper prediction handling.
                    break;
                }

                receiveQueue.RemoveAt(0);
                ReceiveInputInternal(queuedInput, queuedFrame);
            }

            if (LastConsumedFrame < frame && LastConsumedFrame < LastReceivedFrame)
            {
                LastConsumedFrame = Math.Min(frame, LastReceivedFrame);
            }

            // Check if the requested frame is not older than the oldest known frame
            if (LastFrame - frame >= Size)
            {
                input = default;
                return false;
            }

            // Update only if we're the receiving side. Sending side has no need for prediction
            if (LastReceivedFrame >= 0)
            {
                long lastUpdatableFrame = Math.Min(frame, LastReceivedFrame + Size - 1);
                Update(lastUpdatableFrame, UpdateOperation.Read);
            }
            else if (frame > LastFrame)
            {
                // Check if the requested frame is not newer than the most recent frame
                input = default;
                return false;
            }

            int frameIndex = (int)(head - (LastFrame - frame) + Size) % Size;
            if (frameIndex < 0)
            {
                frameIndex = (head - 1 + Size) % Size;
            }
            input = inputs[frameIndex];

            DebugPrintBuffer(nameof(TryGetInput));
            logger.Trace($"{nameof(TryGetInput)}: Found input", (nameof(frameIndex), frameIndex),
                (nameof(input), input), (nameof(frame), frames[frameIndex]));

            return true;
        }

        /// <summary>
        ///     Adds an input to the buffer for a given frame. If the <see cref="Delay" /> is set, the <paramref name="frame" /> is
        ///     offset by its value. This method should be used by the owner of the input; receiving side should use the
        ///     <see cref="ReceiveInput" /> method.
        /// </summary>
        /// <returns>
        ///     True if input differs from the last input in the buffer.
        /// </returns>
        public bool AddInput(in T input, long frame)
        {
            long targetFrame = frame + delay;

            bool isFirstAddition = LastFrame < 0;
            if (isFirstAddition)
            {
                // Set last sent frame to one before so we don't send empty inputs
                LastSentFrame = targetFrame - 1;
            }

            // First case is when there was no change in delay - input frame is always
            // newer so we just append the input. In the second case
            if (targetFrame > LastFrame)
            {
                // We are at the newer frame so just append
                return Append(input, targetFrame);
            }

            // This should happen only if the delay was lowered
            if (!inputComparer.Equals(input, LastInput))
            {
                // The delay was lowered since the last input was added and so we must check
                // whether we align with the previous input. If so, we do nothing as the
                // input was already there for the target frame. If not, we have to append
                // the input to the front even though the target frame is lower, otherwise
                // we could overwrite an input that was already sent. This means that the
                // delay takes effect only as soon as the new inputs align with the old ones.
                return Append(input, LastFrame + 1);
            }

            return false;
        }

        /// <inheritdoc cref="ReceiveInputInternal"/>
        public bool ReceiveInput(in T input, long frame)
        {
            bool isFirstInput = LastReceivedFrame == -1;
            if (isFirstInput)
            {
                LastConsumedFrame = frame - 1;
                LastReceivedFrame = frame - 1;
            }

            bool isOldOrDuplicateInput = frame <= LastReceivedFrame;
            if (isOldOrDuplicateInput)
            {
                return false;
            }

            try
            {
                receiveQueue.Add(frame, input);
            }
            catch
            {
                // Duplicate input, just drop
                return false;
            }

            bool hadMispredictions = false;

            while (receiveQueue.Count > 0)
            {
                T queuedInput = receiveQueue.Values[0];
                long queuedFrame = receiveQueue.Keys[0];

                if (requiresSubsequentFrames)
                {
                    bool isSubsequentInput = queuedFrame == LastReceivedFrame + 1;
                    if (!isSubsequentInput)
                    {
                        break;
                    }
                }

                if (queuedFrame - LastConsumedFrame < Size)
                {
                    receiveQueue.RemoveAt(0);
                    hadMispredictions |= ReceiveInputInternal(queuedInput, queuedFrame);
                }
                else
                {
                    FillWithLastInputForNonSubsequent();
                    break;
                }
            }

            return hadMispredictions;
        }

        /// <summary>
        ///     Adds an input to the buffer for a given frame updating any predicted inputs and marking the
        ///     <see cref="MispredictionFrame" /> if needed. This method should be used by the receiving side; owner of the
        ///     input should use the <see cref="AddInput" /> method.
        /// </summary>
        /// <returns>
        ///     True if there was a misprediction or the input is stale.
        /// </returns>
        private bool ReceiveInputInternal(in T input, long frame)
        {
            DebugPrintBuffer(nameof(ReceiveInput));
            logger.Trace(nameof(ReceiveInput), (nameof(frame), frame), (nameof(input), input), (nameof(LastFrame), LastFrame));

            // Receiving this frame would mean overwriting not yet consumed inputs
            if (frame - LastConsumedFrame >= Size)
            {
                receiveQueue.Add(frame, input);
                return false;
            }

            // Inputs out of order are queued
            if (requiresSubsequentFrames && frame != LastReceivedFrame + 1)
            {
                receiveQueue.Add(frame, input);
                return false;
            }

            if (frame > LastReceivedFrame)
            {
                LastReceivedFrame = frame;
            }

            bool isStaleInput = LastFrame - frame >= Size;
            if (isStaleInput)
            {
                // We ignore all stale inputs before any non-stale input received
                if (hasReceivedNonStaleInput)
                {
                    OnStaleInput?.Invoke(input, frame);
                }
                return true;
            }

            if (!hasReceivedNonStaleInput)
            {
                // First non-stale input received, from now on we'll notify about stale inputs
                hasReceivedNonStaleInput = true;
            }

            bool hasMisprediction = false;
            if (frame <= LastFrame)
            {
                hasMisprediction = Rewrite(input, frame);
            }
            else
            {
                Append(input, frame);
            }

            if (!hasMisprediction)
            {
                long ackFrame = LastReceivedFrame;
                if (MispredictionFrame.HasValue)
                {
                    ackFrame = Math.Min(LastReceivedFrame, MispredictionFrame.Value - 1);
                }

                LastAcknowledgedFrame = ackFrame;
            }

            DebugPrintBuffer(nameof(ReceiveInput));
            logger.Trace($"{nameof(ReceiveInput)}: Input received", (nameof(hasMisprediction), hasMisprediction),
                (nameof(input), input), (nameof(frame), frame));

            return hasMisprediction;
        }

        /// <summary>
        /// Fills buffer with the latest known input, progressing as far as possible without losing
        /// the last consumed frame. Works only when <see cref="requiresSubsequentFrames"/> is false.
        /// </summary>
        private void FillWithLastInputForNonSubsequent()
        {
            // If we require subsequent frames then rolling buffer forward would mean
            // input loss...
            if (!requiresSubsequentFrames)
            {
                // However when subsequent frames are not required we just
                // roll the buffer forward with latest known input. Any out-of-order
                // inputs will be simply dropped.
                ReceiveInputInternal(LastInput, LastConsumedFrame + Size - 1);
            }
        }

        /// <summary>
        ///     Rewrites predicted inputs with a new input that was received.
        /// </summary>
        /// <remarks>
        ///     Example:
        ///     InputFrame: 2, CurrentFrame: 4,
        ///     Buffer: [ 0, 1, 2, 3, 4] => [ 0, 1, 2*, 3*, 4*]
        /// </remarks>
        private bool Rewrite(in T newInput, long frame)
        {
            if (frame > LastFrame)
            {
                logger.Error(Error.ToolkitInputBufferRewrite,
                    (nameof(frame), frame),
                    (nameof(LastFrame), LastFrame),
                    (nameof(Size), Size));
            }

            int framesToUpdate = (int)(LastFrame - frame);

            int frameIndex = (head - framesToUpdate + Size) % Size;
            head = frameIndex;

            // Since we're adding all inputs in an order, prediction mismatch
            // may happen only on the first input that we're changing.
            bool hasNewMisprediction = !inputComparer.Equals(newInput, LastInput) && !MispredictionFrame.HasValue;
            if (hasNewMisprediction)
            {
                MispredictionFrame = LastFrame;
            }

            inputs[head] = newInput;

            for (int i = 0; i < framesToUpdate; i++)
            {
                MoveHead();
                inputs[head] = newInput;
            }

            return hasNewMisprediction;
        }

        /// <summary>
        ///     Appends input to the buffer predicting any inputs in between the last stored input and the appended one.
        /// </summary>
        private bool Append(in T input, long frame)
        {
            if (frame <= LastFrame)
            {
                logger.Error(Error.ToolkitInputBufferAppend,
                    (nameof(frame), frame),
                    (nameof(LastFrame), LastFrame),
                    (nameof(Size), Size));
            }

            Update(frame, UpdateOperation.Write);

            bool inputChanged = !inputComparer.Equals(input, LastInput);

            MoveHead();
            inputs[head] = input;
            frames[head] = frame;

            return inputChanged;
        }

        /// <summary>
        ///     Updates the input buffer by filling it up to the <paramref name="currentFrame"/> using input from the last frame.
        /// </summary>
        private void Update(long currentFrame, UpdateOperation operation)
        {
            // On read we overwrite everything up to the current frame while
            // on write we leave the last input as it will be written to.
            int writeMod = operation == UpdateOperation.Write ? -1 : 0;

            if (head >= 0 && LastFrame >= (currentFrame + writeMod))
            {
                return;
            }

            T input = LastInput;

            // Completely overwrite buffer with last known input
            // Eg: size: 3, lastFrame: 10, currentFrame: 15
            // Read: [ 9, 10, 8 ] -> [ 15, 13, 14 ]
            // Write: [ 9, 10, 8 ] -> [ 9, 13, 14 ]
            if (currentFrame - LastFrame > Size)
            {
                head = 0;

                for (int i = 0; i < Size + writeMod; i++)
                {
                    MoveHead();
                    inputs[head] = input;
                    frames[head] = currentFrame - Size + i + 1;
                }

                return;
            }

            // Copy last input for up to the current frame
            // Eg: size: 3, lastFrame: 10, currentFrame: 12
            // Read: [ 9, 10, 8 ] -> [ 9, 10, 11 ]
            // Write: [ 9, 10, 8 ] -> [ 12, 10, 11 ]
            int lastInputCopyLen = (int)(currentFrame - LastFrame);
            for (int i = 0; i < lastInputCopyLen + writeMod; i++)
            {
                MoveHead();
                inputs[head] = input;
                frames[head] = currentFrame - lastInputCopyLen + i + 1;
            }

            logger.Trace($"{nameof(Update)}", (nameof(LastFrame), LastFrame),
                (nameof(currentFrame), currentFrame));
        }

        private void MoveHead()
        {
            head = (head + 1) % Size;
        }

        /// <inheritdoc cref="IInputBuffer.TryPeekInput"/>
        bool IInputBuffer.TryPeekInput(long frame, out object input)
        {
            bool isWithinBufferRange = LastFrame - Size < frame && frame <= LastFrame;
            if (!isWithinBufferRange)
            {
                input = default;
                return false;
            }

            int frameIndex = (int)(head - (LastFrame - frame) + Size) % Size;
            if (frameIndex < 0)
            {
                frameIndex = (head - 1 + Size) % Size;
            }

            input = inputs[frameIndex];
            return true;
        }

        [Conditional(LogConditionals.Trace)]
        private void DebugPrintBuffer(string operationName)
        {
            ((IInputBufferDebug)this).DebugPrint(operationName, true);
        }

        void IInputBufferDebug.DebugPrint(string operationName, bool includeInputs)
        {
            StringBuilder logBuilder = new StringBuilder(512);
            logBuilder.Append($"{operationName}: Buffer: [ ");
            for (int i = 0; i < Size; i++)
            {
                logBuilder.Append($"\n\t{(i == head ? "@" : "#")}{frames[i]}");
                if (includeInputs)
                {
                    logBuilder.Append($" ({inputs[i]})");
                }

                if (i != Size - 1)
                {
                    logBuilder.Append(" | ");
                }
            }
            logBuilder.Append(" ]");
            logBuilder.Append($"\n\tLastReceived: {LastReceivedFrame}, LastFrame: {LastFrame}, LastSent: {LastSentFrame}, " +
                              $"MispredictionFrame: {MispredictionFrame}, LastConsumed: {LastConsumedFrame}, " +
                              $"QueueCount: {receiveQueue.Count}");

            string log = logBuilder.ToString();
            logger.Trace(log, (nameof(head), head));
        }
    }
}
