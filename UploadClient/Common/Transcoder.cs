using System.Runtime.InteropServices;
using System.Text;

namespace UploadClient
{
    public unsafe class Transcoder
    {
        [DllImport("TCProxy.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int CreateTransCode();

        [DllImport("TCProxy.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int OpenTranscoder();

        [DllImport("TCProxy.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int CloseTranscoder();

        [DllImport("TCProxy.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int StopTranscode();

        [DllImport("TCProxy.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Transcode(string srcInput, string output, int profileID);

        [DllImport("TCProxy.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetState(int* progress, int* state);

        [DllImport("TCProxy.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetOutputFileName(StringBuilder output_filename);
    }
}
