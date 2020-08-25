using DShowNET;
using DShowNET.Device;
using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;

namespace OcrClassLibrary {
    public class Capture : ISampleGrabberCB {
        /// <summary> flag to detect first Form appearance </summary>
        private bool firstActive;

        /// <summary> base filter of the actually used video devices. </summary>
        private IBaseFilter capFilter;

        /// <summary> graph builder interface. </summary>
        private IGraphBuilder graphBuilder;

        /// <summary> capture graph builder interface. </summary>
        private ICaptureGraphBuilder2 capGraph;
        private ISampleGrabber sampGrabber;

        /// <summary> control interface. </summary>
        private IMediaControl mediaCtrl;

        /// <summary> event interface. </summary>
        private IMediaEventEx mediaEvt;

        /// <summary> video window interface. </summary>
        private IVideoWindow videoWin;

        /// <summary> grabber filter interface. </summary>
        private IBaseFilter baseGrabFlt;

        /// <summary> structure describing the bitmap to grab. </summary>
        private VideoInfoHeader videoInfoHeader;
        private bool captured = true;
        private int bufferedSize;

        /// <summary> buffer for bitmap data. </summary>
        private byte[] savedArray;

        /// <summary> list of installed video devices. </summary>
        private ArrayList capDevices;

        private const int WM_GRAPHNOTIFY = 0x00008001;	// message from graph

        private const int WS_CHILD = 0x40000000;	// attributes for video window
        private const int WS_CLIPCHILDREN = 0x02000000;
        private const int WS_CLIPSIBLINGS = 0x04000000;

        /// <summary> event when callback has finished (ISampleGrabberCB.BufferCB). </summary>
        private delegate void CaptureDone();

        private int rotCookie = 0;

        internal enum PlayState {
            Init, Stopped, Paused, Running
        }

        private Boolean processingCaptureFlag;
        /// <summary>
        /// キャプチャ処理中かどうか
        /// </summary>
        protected Boolean IsProcessingCapture {
            get {
                return processingCaptureFlag;
            }
        }

        private Bitmap captureImage;

        private ImageFormat fileFormat;
        /// <summary>
        /// キャプチャイメージを保存するフォーマット
        /// </summary>
        protected ImageFormat FileFormat {
            set {
                if (!IsProcessingCapture) fileFormat = value;
            }
            get {
                return fileFormat;
            }
        }

        public void Initialize(int deviceNum) {
            if (firstActive)
                return;
            firstActive = true;

            if (!DsUtils.IsCorrectDirectXVersion()) {
                return;
            }

            if (!DsDev.GetDevicesOfCat(FilterCategory.VideoInputDevice, out capDevices)) {
                return;
            }

            DsDevice dev = null;
            if (capDevices.Count >= 1)
                dev = capDevices[deviceNum] as DsDevice;

            if (dev == null) {
                return;
            }

            StartupVideo(dev.Mon);
        }

        /// <summary>
        /// Capture Image
        /// </summary>
        /// <returns></returns>
        public Bitmap CaptureImage() {
            int hr;
            if (sampGrabber == null)
                return null;

            if (savedArray == null) {
                int size = videoInfoHeader.BmiHeader.ImageSize;
                if ((size < 1000) || (size > 16000000))
                    return null;
                savedArray = new byte[size + 64000];
            }

            captured = false;
            processingCaptureFlag = true;
            hr = sampGrabber.SetCallback(this, 1);
            while (processingCaptureFlag) {
                Thread.Sleep(100);
            }
            hr = sampGrabber.SetCallback(null, 0);
            return new Bitmap(captureImage);
        }

        /// <summary>
        /// 撮影が終わった時
        /// </summary>
        protected void OnCaptureDone() {
            try {
                int hr;
                if (sampGrabber == null)
                    return;

                int w = videoInfoHeader.BmiHeader.Width;
                int h = videoInfoHeader.BmiHeader.Height;
                if (((w & 0x03) != 0) || (w < 32) || (w > 4096) || (h < 32) || (h > 4096))
                    return;
                int stride = w * 3;

                GCHandle handle = GCHandle.Alloc(savedArray, GCHandleType.Pinned);
                int scan0 = (int)handle.AddrOfPinnedObject();
                scan0 += (h - 1) * stride;
                captureImage = new Bitmap(w, h, -stride, PixelFormat.Format24bppRgb, (IntPtr)scan0);
                captureImage.Save(@"C:\Temp2\Mandrill.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);

                handle.Free();


                savedArray = null;
                processingCaptureFlag = false;
            }
            catch (Exception ee) {
            }
        }

        private bool StartupVideo(UCOMIMoniker mon) {
            int hr;
            try {
                if (!CreateCaptureDevice(mon))
                    return false;

                if (!GetInterfaces())
                    return false;

                if (!SetupGraph())
                    return false;

                if (!SetupVideoWindow())
                    return false;

#if DEBUG
                //DsROT.AddGraphToRot(graphBuilder, out rotCookie);		// graphBuilder capGraph
#endif

                hr = mediaCtrl.Run();
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                //bool hasTuner = DsUtils.ShowTunerPinDialog(capGraph, capFilter, this.Handle);

                return true;
            }
            catch (Exception ee) {
                return false;
            }
        }

        /// <summary> make the video preview window to show in videoPanel. </summary>
        private bool SetupVideoWindow() {
            int hr;
            try {
                // Set the video window to be a child of the main window
                //hr = videoWin.put_Owner(videoPanel.Handle);
                //if (hr < 0)
                //    Marshal.ThrowExceptionForHR(hr);

                // Set video window style
                hr = videoWin.put_WindowStyle(WS_CHILD | WS_CLIPCHILDREN);
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                // Use helper function to position video window in client rect of owner window
                //ResizeVideoWindow();

                // Make the video window visible, now that it is properly positioned
                hr = videoWin.put_Visible(DsHlp.OATRUE);
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                //hr = mediaEvt.SetNotifyWindow(this.Handle, WM_GRAPHNOTIFY, IntPtr.Zero);
                //if (hr < 0)
                //    Marshal.ThrowExceptionForHR(hr);
                return true;
            }
            catch (Exception ee) {
                return false;
            }
        }

        private bool SetupGraph() {
            int hr;
            try {
                hr = capGraph.SetFiltergraph(graphBuilder);
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                hr = graphBuilder.AddFilter(capFilter, "Ds.NET Video Capture Device");
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                //DsUtils.ShowCapPinDialog(capGraph, capFilter, this.Handle);

                AMMediaType media = new AMMediaType();
                media.majorType = MediaType.Video;
                media.subType = MediaSubType.RGB24;
                media.formatType = FormatType.VideoInfo;		// ???
                hr = sampGrabber.SetMediaType(media);
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                hr = graphBuilder.AddFilter(baseGrabFlt, "Ds.NET Grabber");
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                Guid cat = PinCategory.Preview;
                Guid med = MediaType.Video;
                hr = capGraph.RenderStream(ref cat, ref med, capFilter, null, null); // baseGrabFlt 
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                cat = PinCategory.Capture;
                med = MediaType.Video;
                hr = capGraph.RenderStream(ref cat, ref med, capFilter, null, baseGrabFlt); // baseGrabFlt 
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                media = new AMMediaType();
                hr = sampGrabber.GetConnectedMediaType(media);
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);
                if ((media.formatType != FormatType.VideoInfo) || (media.formatPtr == IntPtr.Zero))
                    throw new NotSupportedException("Unknown Grabber Media Format");

                videoInfoHeader = (VideoInfoHeader)Marshal.PtrToStructure(media.formatPtr, typeof(VideoInfoHeader));
                Marshal.FreeCoTaskMem(media.formatPtr); media.formatPtr = IntPtr.Zero;

                hr = sampGrabber.SetBufferSamples(false);
                if (hr == 0)
                    hr = sampGrabber.SetOneShot(false);
                if (hr == 0)
                    hr = sampGrabber.SetCallback(null, 0);
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                return true;
            }
            catch (Exception ee) {
                return false;
            }
        }

        private bool GetInterfaces() {
            Type comType = null;
            object comObj = null;
            try {
                comType = Type.GetTypeFromCLSID(Clsid.FilterGraph);
                if (comType == null)
                    throw new NotImplementedException(@"DirectShow FilterGraph not installed/registered!");
                comObj = Activator.CreateInstance(comType);
                graphBuilder = (IGraphBuilder)comObj; comObj = null;

                Guid clsid = Clsid.CaptureGraphBuilder2;
                Guid riid = typeof(ICaptureGraphBuilder2).GUID;
                comObj = DsBugWO.CreateDsInstance(ref clsid, ref riid);
                capGraph = (ICaptureGraphBuilder2)comObj; comObj = null;

                comType = Type.GetTypeFromCLSID(Clsid.SampleGrabber);
                if (comType == null)
                    throw new NotImplementedException(@"DirectShow SampleGrabber not installed/registered!");
                comObj = Activator.CreateInstance(comType);
                sampGrabber = (ISampleGrabber)comObj; comObj = null;

                mediaCtrl = (IMediaControl)graphBuilder;
                videoWin = (IVideoWindow)graphBuilder;
                mediaEvt = (IMediaEventEx)graphBuilder;
                baseGrabFlt = (IBaseFilter)sampGrabber;
                return true;
            }
            catch (Exception ee) {
                return false;
            }
            finally {
                if (comObj != null)
                    Marshal.ReleaseComObject(comObj); comObj = null;
            }
        }

        private bool CreateCaptureDevice(UCOMIMoniker mon) {
            object capObj = null;
            try {
                Guid gbf = typeof(IBaseFilter).GUID;
                mon.BindToObject(null, null, ref gbf, out capObj);
                capFilter = (IBaseFilter)capObj; capObj = null;
                return true;
            }
            catch (Exception ee) {
                return false;
            }
            finally {
                if (capObj != null)
                    Marshal.ReleaseComObject(capObj); capObj = null;
            }

        }

        /// <summary> do cleanup and release DirectShow. </summary>
        public void CloseInterfaces() {
            int hr;
            try {
#if DEBUG
                if (rotCookie != 0)
                    DsROT.RemoveGraphFromRot(ref rotCookie);
#endif

                if (mediaCtrl != null) {
                    hr = mediaCtrl.Stop();
                    mediaCtrl = null;
                }

                if (mediaEvt != null) {
                    hr = mediaEvt.SetNotifyWindow(IntPtr.Zero, WM_GRAPHNOTIFY, IntPtr.Zero);
                    mediaEvt = null;
                }

                if (videoWin != null) {
                    hr = videoWin.put_Visible(DsHlp.OAFALSE);
                    hr = videoWin.put_Owner(IntPtr.Zero);
                    videoWin = null;
                }

                baseGrabFlt = null;
                if (sampGrabber != null)
                    Marshal.ReleaseComObject(sampGrabber); sampGrabber = null;

                if (capGraph != null)
                    Marshal.ReleaseComObject(capGraph); capGraph = null;

                if (graphBuilder != null)
                    Marshal.ReleaseComObject(graphBuilder); graphBuilder = null;

                if (capFilter != null)
                    Marshal.ReleaseComObject(capFilter); capFilter = null;

                if (capDevices != null) {
                    foreach (DsDevice d in capDevices)
                        d.Dispose();
                    capDevices = null;
                }
            }
            catch (Exception) { }
        }

        protected void OnGraphNotify() {
            DsEvCode code;
            int p1, p2, hr = 0;
            do {
                hr = mediaEvt.GetEvent(out code, out p1, out p2, 0);
                if (hr < 0)
                    break;
                hr = mediaEvt.FreeEventParams(code, p1, p2);
            }
            while (hr == 0);
        }

        /// <summary> sample callback, NOT USED. </summary>
        int ISampleGrabberCB.SampleCB(double SampleTime, IMediaSample pSample) {
            return 0;
        }

        /// <summary> buffer callback, COULD BE FROM FOREIGN THREAD. </summary>
        int ISampleGrabberCB.BufferCB(double SampleTime, IntPtr pBuffer, int BufferLen) {
            if (captured || (savedArray == null)) {
                return 0;
            }

            captured = true;
            bufferedSize = BufferLen;
            if ((pBuffer != IntPtr.Zero) && (BufferLen > 1000) && (BufferLen <= savedArray.Length))
                Marshal.Copy(pBuffer, savedArray, 0, BufferLen);
            //this.BeginInvoke(new CaptureDone(this.OnCaptureDone));
            this.OnCaptureDone();
            return 0;
        }


    }
}
