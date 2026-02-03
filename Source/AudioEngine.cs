using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace NoiseGen
{
    public class AudioEngine : IDisposable
    {
        const int MMSYSERR_NOERROR = 0;
        const int CALLBACK_FUNCTION = 0x00030000;
        const int WAVE_MAPPER = -1;
        const uint WHDR_DONE = 0x00000001;

        [StructLayout(LayoutKind.Sequential)]
        public struct WAVEFORMATEX
        {
            public ushort wFormatTag;
            public ushort nChannels;
            public uint nSamplesPerSec;
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort wBitsPerSample;
            public ushort cbSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WAVEHDR
        {
            public IntPtr lpData;
            public uint dwBufferLength;
            public uint dwBytesRecorded;
            public IntPtr dwUser;
            public uint dwFlags;
            public uint dwLoops;
            public IntPtr lpNext;
            public IntPtr reserved;
        }

        private delegate void WaveCallback(IntPtr hWaveOut, uint uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2);

        [DllImport("winmm.dll")]
        private static extern int waveOutOpen(out IntPtr hWaveOut, int uDeviceID, ref WAVEFORMATEX lpFormat, WaveCallback dwCallback, IntPtr dwInstance, int fdwOpen);

        [DllImport("winmm.dll")]
        private static extern int waveOutPrepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int uSize);

        [DllImport("winmm.dll")]
        private static extern int waveOutWrite(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int uSize);

        [DllImport("winmm.dll")]
        private static extern int waveOutUnprepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int uSize);

        [DllImport("winmm.dll")]
        private static extern int waveOutClose(IntPtr hWaveOut);
        
        [DllImport("winmm.dll")]
        private static extern int waveOutReset(IntPtr hWaveOut);

        private IntPtr _hWaveOut = IntPtr.Zero;
        private WaveCallback _callback;
        
        // Unmanaged memory set
        private IntPtr[] _headerPtrs; // Pointers to unmanaged WAVEHDR structs
        private GCHandle[] _bufferHandles; // Pinned managed arrays for audio data
        private short[][] _buffers; 
        
        private float[] _mixBuffer;
        private int _bufferCount = 4;
        private int _bufferSize;
        private int _sampleRate = 44100;

        public List<IGenerator> Generators = new List<IGenerator>();
        public float MasterVolume { get; set; }
        
        private Queue<int> _freeBuffers = new Queue<int>();
        private object _lock = new object();

        public AudioEngine(int sampleRate = 44100, int bufferLatencyMs = 50)
        {
            MasterVolume = 1.0f;
            _sampleRate = sampleRate;
            int channels = 2;
            
            _bufferSize = (int)(sampleRate * (bufferLatencyMs / 1000.0) * channels);
            if (_bufferSize % 2 != 0) _bufferSize++;

            _buffers = new short[_bufferCount][];
            _mixBuffer = new float[_bufferSize];
            _headerPtrs = new IntPtr[_bufferCount];
            _bufferHandles = new GCHandle[_bufferCount];

            WAVEFORMATEX fmt = new WAVEFORMATEX();
            fmt.wFormatTag = 1;
            fmt.nChannels = (ushort)channels;
            fmt.nSamplesPerSec = (uint)sampleRate;
            fmt.wBitsPerSample = 16;
            fmt.nBlockAlign = (ushort)(channels * 2);
            fmt.nAvgBytesPerSec = fmt.nSamplesPerSec * fmt.nBlockAlign;
            fmt.cbSize = 0;

            _callback = new WaveCallback(OnWaveOutProc);

            int res = waveOutOpen(out _hWaveOut, WAVE_MAPPER, ref fmt, _callback, IntPtr.Zero, CALLBACK_FUNCTION);
            if (res != MMSYSERR_NOERROR) throw new Exception("waveOutOpen failed: " + res);

            int hdrSize = Marshal.SizeOf(typeof(WAVEHDR));

            for (int i = 0; i < _bufferCount; i++)
            {
                _buffers[i] = new short[_bufferSize];
                _bufferHandles[i] = GCHandle.Alloc(_buffers[i], GCHandleType.Pinned);
                
                // Allocate unmanaged header
                _headerPtrs[i] = Marshal.AllocHGlobal(hdrSize);
                
                WAVEHDR hdr = new WAVEHDR();
                hdr.lpData = _bufferHandles[i].AddrOfPinnedObject();
                hdr.dwBufferLength = (uint)(_bufferSize * 2);
                hdr.dwFlags = 0;
                
                Marshal.StructureToPtr(hdr, _headerPtrs[i], false);
                
                waveOutPrepareHeader(_hWaveOut, _headerPtrs[i], hdrSize);
                _freeBuffers.Enqueue(i);
            }
        }

        // Just keep reference to prevent GC
        private void OnWaveOutProc(IntPtr hWaveOut, uint uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2) {}

        public void Update()
        {
            if (_hWaveOut == IntPtr.Zero) return;

            lock (_lock)
            {
                // Check active buffers
                for (int i = 0; i < _bufferCount; i++)
                {
                    if (!_freeBuffers.Contains(i))
                    {
                        // Read flags from unmanaged memory
                        // We only need the dwFlags field. Offset of dwFlags in WAVEHDR?
                        // IntPtr (4/8) + uint (4) + uint (4) + IntPtr (4/8) = ...
                        // Safer to Marshal back.
                        WAVEHDR hdr = (WAVEHDR)Marshal.PtrToStructure(_headerPtrs[i], typeof(WAVEHDR));
                        
                        if ((hdr.dwFlags & WHDR_DONE) == WHDR_DONE)
                        {
                            _freeBuffers.Enqueue(i);
                        }
                    }
                }

                while (_freeBuffers.Count > 0)
                {
                    int idx = _freeBuffers.Dequeue();
                    FillAndSubmitBuffer(idx);
                }
            }
        }

        private void FillAndSubmitBuffer(int idx)
        {
            Array.Clear(_mixBuffer, 0, _mixBuffer.Length);

            bool anyActive = false;
            foreach (var gen in Generators)
            {
                if (gen.Enabled && gen.Volume > 0)
                {
                    gen.FillBuffer(_mixBuffer, 0, _mixBuffer.Length, _sampleRate);
                    anyActive = true;
                }
            }

            // If nothing active, we still write silence (cleared buffer), otherwise output stops
            // and buffers don't cycle.
            
            short[] pcm = _buffers[idx];
            for (int i = 0; i < _mixBuffer.Length; i++)
            {
                float val = _mixBuffer[i] * MasterVolume;
                if (val > 1.0f) val = 1.0f;
                if (val < -1.0f) val = -1.0f;
                pcm[i] = (short)(val * 32767);
            }

            // Update Unmanaged Header Flags?
            // waveOutWrite resets WHDR_DONE, but generally we don't need to manually touch flags 
            // EXCEPT ensuring it's not marked DONE before we send (which it shouldn't be if we just got it back?)
            // Actually, we don't need to write structure back if we didn't change pointer/length.
            
            int res = waveOutWrite(_hWaveOut, _headerPtrs[idx], Marshal.SizeOf(typeof(WAVEHDR)));
            if (res != MMSYSERR_NOERROR)
            {
                 // Log?
            }
        }

        public void Dispose()
        {
            if (_hWaveOut != IntPtr.Zero)
            {
                waveOutReset(_hWaveOut);
                for (int i = 0; i < _bufferCount; i++)
                {
                    waveOutUnprepareHeader(_hWaveOut, _headerPtrs[i], Marshal.SizeOf(typeof(WAVEHDR)));
                    Marshal.FreeHGlobal(_headerPtrs[i]);
                    if (_bufferHandles[i].IsAllocated) _bufferHandles[i].Free();
                }
                waveOutClose(_hWaveOut);
                _hWaveOut = IntPtr.Zero;
            }
        }
    }
}
