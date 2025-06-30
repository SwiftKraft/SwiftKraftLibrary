using System;
using System.Collections.Generic;

namespace SwiftKraft.Utils
{
    public class SerialGenerator
    {
        private uint _nextSerial = 1;
        private readonly Stack<uint> _recycledSerials = new();

        /// <summary>
        /// Returns the next available serial ID, reusing destroyed ones if available.
        /// </summary>
        public uint NextSerial()
        {
            if (_recycledSerials.Count > 0)
            {
                return _recycledSerials.Pop();
            }

            // Overflow protection (optional)
            if (_nextSerial == uint.MaxValue)
                throw new Exception("Serial overflow.");

            return _nextSerial++;
        }

        /// <summary>
        /// Recycles a serial ID that is no longer in use.
        /// </summary>
        public void RecycleSerial(uint serial)
        {
            if (serial <= 0 || serial >= _nextSerial || _recycledSerials.Contains(serial))
                return;

            _recycledSerials.Push(serial);
        }

        /// <summary>
        /// Resets the generator (optional for testing or world wipes).
        /// </summary>
        public void Reset()
        {
            _nextSerial = 1;
            _recycledSerials.Clear();
        }
    }
}