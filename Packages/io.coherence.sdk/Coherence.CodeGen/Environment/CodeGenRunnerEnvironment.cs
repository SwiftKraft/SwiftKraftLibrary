// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#if UNITY_5_3_OR_NEWER
// IMPORTANT: Used by the pure-dotnet client, DON'T REMOVE.
#define UNITY
#endif

namespace Coherence.CodeGen
{
    using System;
    using Log;

    public class CodeGenRunnerEnvironment
    {
        protected virtual void PrepareCodeGen() {}
        protected virtual void FinalizeCodeGen() {}

        public virtual bool IsReady()
        {
            return true;
        }

        public CodeGenGuard WrapCodeGen()
        {
            return new CodeGenGuard(this);
        }

        public static CodeGenRunnerEnvironment Create(Logger logger)
        {
#if UNITY
            return new CodeGenRunnerUnityEnvironment(logger);
#else
            return new CodeGenRunnerEnvironment();
#endif
        }

        public readonly struct CodeGenGuard : IDisposable
        {
            private readonly CodeGenRunnerEnvironment environment;

            public CodeGenGuard(CodeGenRunnerEnvironment environment)
            {
                this.environment = environment;
                environment.PrepareCodeGen();
            }

            public void Dispose()
            {
                environment.FinalizeCodeGen();
            }
        }
    }
}
