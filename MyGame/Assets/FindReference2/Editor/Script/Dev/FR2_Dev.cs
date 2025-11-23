// #define FR2_DEV
using System;

namespace vietlabs.fr2
{
    public class FR2_Dev
    {
        public static IDisposable NoLog => new NoLogScope();

        private readonly struct NoLogScope : IDisposable
        {
#if FR2_DEV
            internal NoLogScope(bool _) { }
            public void Dispose() { }
#else
            private readonly bool _saved;
            internal NoLogScope(bool _)
            {
                _saved = UnityEngine.Debug.unityLogger.logEnabled;
                UnityEngine.Debug.unityLogger.logEnabled = false;
            }
            public void Dispose() => UnityEngine.Debug.unityLogger.logEnabled = _saved;
#endif
        }
    }
}